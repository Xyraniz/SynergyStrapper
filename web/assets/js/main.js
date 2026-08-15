/* Núcleo Modular: navegación sobria, accesible y con movimiento contenido. */
(() => {
  const header = document.querySelector('[data-header]');
  const toggle = document.querySelector('.nav-toggle');
  const navigation = document.querySelector('.site-nav');
  const navigationLinks = document.querySelectorAll('.site-nav__link');

  const setHeaderState = () => {
    if (header) header.classList.toggle('is-scrolled', window.scrollY > 12);
  };

  setHeaderState();
  window.addEventListener('scroll', setHeaderState, { passive: true });

  if (toggle && navigation) {
    toggle.addEventListener('click', () => {
      const isOpen = toggle.getAttribute('aria-expanded') === 'true';
      toggle.setAttribute('aria-expanded', String(!isOpen));
      navigation.classList.toggle('is-open', !isOpen);
    });

    navigationLinks.forEach((link) => {
      link.addEventListener('click', () => {
        toggle.setAttribute('aria-expanded', 'false');
        navigation.classList.remove('is-open');
      });
    });
  }

  const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  const revealItems = document.querySelectorAll('.reveal');

  if (reducedMotion || !('IntersectionObserver' in window)) {
    revealItems.forEach((item) => item.classList.add('is-visible'));
    return;
  }

  const observer = new IntersectionObserver(
    (entries, currentObserver) => {
      entries.forEach((entry) => {
        if (!entry.isIntersecting) return;
        entry.target.classList.add('is-visible');
        currentObserver.unobserve(entry.target);
      });
    },
    { threshold: 0.12, rootMargin: '0px 0px -22px' }
  );

  revealItems.forEach((item) => observer.observe(item));
})();
