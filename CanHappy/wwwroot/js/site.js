(() => {
	const storageKey = "canhappy-theme";
	const lastTopCategoryKey = "canhappy-last-top-category";
	const root = document.documentElement;
	const button = document.getElementById("theme-toggle");
	const topCategoryLinks = Array.from(document.querySelectorAll(".top-category-link"));

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

	const setLastTopCategory = (targetUrl) => {
		topCategoryLinks.forEach((link) => {
			const linkUrl = `${link.pathname}${link.search}`;
			link.classList.toggle("is-last-visited", linkUrl === targetUrl);
		});
	};

	const savedTopCategory = localStorage.getItem(lastTopCategoryKey);
	if (savedTopCategory) {
		setLastTopCategory(savedTopCategory);
	}

	topCategoryLinks.forEach((link) => {
		link.addEventListener("click", () => {
			const targetUrl = `${link.pathname}${link.search}`;
			localStorage.setItem(lastTopCategoryKey, targetUrl);
			setLastTopCategory(targetUrl);
		});
	});
})();
