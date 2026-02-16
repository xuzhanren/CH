(() => {
	const storageKey = "canhappy-theme";
	const root = document.documentElement;
	const button = document.getElementById("theme-toggle");

	const applyTheme = (theme) => {
		root.setAttribute("data-bs-theme", theme);
		if (button) {
			button.textContent = theme === "dark" ? "☀️" : "🌙";
		}
	};

	const savedTheme = localStorage.getItem(storageKey) || "light";
	applyTheme(savedTheme);

	button?.addEventListener("click", () => {
		const nextTheme = root.getAttribute("data-bs-theme") === "dark" ? "light" : "dark";
		localStorage.setItem(storageKey, nextTheme);
		applyTheme(nextTheme);
	});
})();
