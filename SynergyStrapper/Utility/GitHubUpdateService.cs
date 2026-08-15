using System.Security.Cryptography;

namespace SynergyStrapper.Utility
{
    /// <summary>
    /// Provides the shared, defensive implementation for Synergy Strapper self-updates.
    /// </summary>
    internal static class GitHubUpdateService
    {
        public const string ExpectedAssetName = "SynergyStrapper.exe";
        private const long MaximumDownloadSize = 250L * 1024L * 1024L;
        private const string Sha256Algorithm = "sha256";

        public static bool TryGetCompatibleAsset(GithubRelease release, out GithubReleaseAsset asset)
        {
            asset = null!;

            if (release is null || release.Draft || release.Prerelease || release.Assets is null)
                return false;

            var candidate = release.Assets.FirstOrDefault(x =>
                x is not null
                && string.Equals(x.Name, ExpectedAssetName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.State, "uploaded", StringComparison.OrdinalIgnoreCase));

            if (candidate is null || string.IsNullOrWhiteSpace(candidate.BrowserDownloadUrl))
                return false;

            asset = candidate;
            return true;
        }

        public static async Task<bool> IsUpdateAvailableAsync(
            GithubRelease release,
            GithubReleaseAsset asset,
            string currentVersion,
            bool isProductionBuild,
            string installedExecutable,
            CancellationToken cancellationToken = default)
        {
            VersionComparison versionComparison;

            try
            {
                versionComparison = Utilities.CompareVersions(currentVersion, release.TagName);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("GitHubUpdateService::IsUpdateAvailableAsync", ex);
                return false;
            }

            if (versionComparison == VersionComparison.GreaterThan)
                return false;

            if (versionComparison == VersionComparison.LessThan || !isProductionBuild)
                return true;

            // A release asset can be replaced without changing its tag. In that case the
            // version is equal, so compare the server-provided digest before deciding.
            if (string.IsNullOrWhiteSpace(asset.Digest) || !File.Exists(installedExecutable))
                return false;

            return await HasDifferentSha256Async(installedExecutable, asset.Digest, cancellationToken);
        }

        public static async Task<string> DownloadAsync(
            GithubReleaseAsset asset,
            string destinationDirectory,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCompatibleAsset(new GithubRelease { Assets = new List<GithubReleaseAsset> { asset } }, out _))
                throw new InvalidDataException("The selected GitHub release asset is not a supported Synergy Strapper executable.");

            Directory.CreateDirectory(destinationDirectory);

            string destinationPath = Path.Combine(destinationDirectory, ExpectedAssetName);
            string temporaryPath = Path.Combine(destinationDirectory, $".{ExpectedAssetName}.{Guid.NewGuid():N}.part");

            try
            {
                using var response = await App.HttpClient.GetAsync(
                    asset.BrowserDownloadUrl,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                long? contentLength = response.Content.Headers.ContentLength;
                ValidateExpectedSize(contentLength, asset.Size);

                await using (Stream input = await response.Content.ReadAsStreamAsync(cancellationToken))
                await using (var output = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 128 * 1024,
                    options: FileOptions.SequentialScan | FileOptions.WriteThrough))
                {
                    var buffer = new byte[128 * 1024];
                    long totalBytes = 0;

                    while (true)
                    {
                        int bytesRead = await input.ReadAsync(buffer.AsMemory(), cancellationToken);
                        if (bytesRead == 0)
                            break;

                        totalBytes += bytesRead;
                        if (totalBytes > MaximumDownloadSize || (asset.Size > 0 && totalBytes > asset.Size))
                            throw new InvalidDataException("The downloaded executable exceeds the expected size.");

                        await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    }

                    if (totalBytes == 0 || (asset.Size > 0 && totalBytes != asset.Size))
                        throw new InvalidDataException("The downloaded executable has an unexpected size.");

                    await output.FlushAsync(cancellationToken);
                }

                string actualDigest = await ComputeSha256Async(temporaryPath, cancellationToken);
                if (!DigestMatches(asset.Digest, actualDigest))
                    throw new InvalidDataException("The downloaded executable failed SHA-256 verification.");

                File.Move(temporaryPath, destinationPath, overwrite: true);
                return destinationPath;
            }
            catch
            {
                TryDelete(temporaryPath);
                throw;
            }
        }

        private static void ValidateExpectedSize(long? contentLength, long expectedSize)
        {
            if (contentLength is <= 0 or > MaximumDownloadSize)
                throw new InvalidDataException("The GitHub asset has an invalid content length.");

            if (expectedSize > 0 && expectedSize > MaximumDownloadSize)
                throw new InvalidDataException("The GitHub asset is larger than the supported download limit.");

            if (contentLength.HasValue && expectedSize > 0 && contentLength.Value != expectedSize)
                throw new InvalidDataException("The GitHub asset size does not match its metadata.");
        }

        private static async Task<bool> HasDifferentSha256Async(
            string filePath,
            string expectedDigest,
            CancellationToken cancellationToken)
        {
            string actualDigest = await ComputeSha256Async(filePath, cancellationToken);
            return !DigestMatches(expectedDigest, actualDigest);
        }

        private static async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
        {
            await using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 128 * 1024,
                options: FileOptions.SequentialScan);

            using var sha256 = SHA256.Create();
            byte[] hash = await sha256.ComputeHashAsync(stream, cancellationToken);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static bool DigestMatches(string? remoteDigest, string actualDigest)
        {
            if (string.IsNullOrWhiteSpace(remoteDigest))
                return true;

            string[] parts = remoteDigest.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || !string.Equals(parts[0], Sha256Algorithm, StringComparison.OrdinalIgnoreCase))
            {
                App.Logger.WriteLine("GitHubUpdateService::DigestMatches", $"Rejecting unsupported release digest format '{remoteDigest}'.");
                return false;
            }

            return string.Equals(parts[1], actualDigest, StringComparison.OrdinalIgnoreCase);
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("GitHubUpdateService::TryDelete", ex);
            }
        }
    }
}
