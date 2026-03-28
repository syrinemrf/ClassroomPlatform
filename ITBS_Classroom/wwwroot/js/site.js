(function () {
    const themeKey = "itbs-theme";
    const body = document.body;
    const toggle = document.getElementById("themeToggle");

    const setTheme = (theme) => {
        const dark = theme === "dark";
        body.classList.toggle("dark-mode", dark);
        if (toggle) {
            const isFr = document.documentElement.lang === "fr";
            toggle.textContent = dark
                ? (isFr ? "Mode clair" : "Light mode")
                : (isFr ? "Mode sombre" : "Dark mode");
        }
    };

    const savedTheme = localStorage.getItem(themeKey) || "light";
    setTheme(savedTheme);

    if (toggle) {
        toggle.addEventListener("click", function () {
            const nextTheme = body.classList.contains("dark-mode") ? "light" : "dark";
            localStorage.setItem(themeKey, nextTheme);
            setTheme(nextTheme);
        });
    }
})();
