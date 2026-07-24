(function () {
    function applyBranding() {
        var information = document.querySelector(".swagger-ui .information-container");
        if (!information || document.getElementById("bs-brand-hero")) {
            return;
        }

        var hero = document.createElement("section");
        hero.id = "bs-brand-hero";
        hero.className = "bs-brand-hero";
        hero.innerHTML = [
            '<div class="bs-brand-row">',
            '  <img class="bs-brand-logo" src="/swagger-ui/logo.png" alt="Blue Sands STEM Labs logo" />',
            '  <div class="bs-brand-copy">',
            '    <h2 class="bs-brand-title">Blue Sands STEM Labs API</h2>',
            '    <p class="bs-brand-subtitle">Secure APIs for Inquiry Learning Spaces (ILS), realtime simulation sessions, assessments, moderation, and teacher analytics.</p>',
            '    <div class="bs-brand-chips">',
            '      <span class="bs-brand-chip">Bearer JWT</span>',
            '      <span class="bs-brand-chip">REST + WebSocket</span>',
            '      <span class="bs-brand-chip">v1</span>',
            "    </div>",
            "  </div>",
            "</div>"
        ].join("");

        information.insertBefore(hero, information.firstChild);

        var topbarLink = document.querySelector(".swagger-ui .topbar a");
        if (topbarLink) {
            topbarLink.setAttribute("href", "https://www.bluesandstemlabs.com");
            topbarLink.setAttribute("target", "_blank");
            topbarLink.setAttribute("rel", "noopener noreferrer");
            topbarLink.title = "Blue Sands STEM Labs";
        }

        var topbarImage = document.querySelector(".swagger-ui .topbar-wrapper img");
        if (topbarImage) {
            topbarImage.src = "/swagger-ui/logo.png";
            topbarImage.alt = "Blue Sands STEM Labs";
            topbarImage.style.maxHeight = "38px";
        }
    }

    function runWhenReady() {
        applyBranding();
        var observer = new MutationObserver(function () {
            applyBranding();
        });
        observer.observe(document.body, { childList: true, subtree: true });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", runWhenReady);
    } else {
        runWhenReady();
    }
})();
