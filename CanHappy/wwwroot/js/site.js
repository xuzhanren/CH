(() => {
	const storageKey = "canhappy-theme";
	const lastTopCategoryKey = "canhappy-last-top-category";
	const lastTopSubcategoryKey = "canhappy-last-top-subcategory";
	const root = document.documentElement;
	const button = document.getElementById("theme-toggle");
	const topCategoryLinks = Array.from(document.querySelectorAll(".top-category-link"));
	const topCategoryDropdowns = Array.from(document.querySelectorAll(".top-category-dropdown"));
	const topSubcategoryLinks = Array.from(document.querySelectorAll(".top-subcategory-link"));

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

	const setLastTopSubcategory = (targetUrl) => {
		topSubcategoryLinks.forEach((link) => {
			const linkUrl = `${link.pathname}${link.search}`;
			link.classList.toggle("is-last-visited", linkUrl === targetUrl);
		});
	};

	const savedTopCategory = localStorage.getItem(lastTopCategoryKey);
	if (savedTopCategory) {
		setLastTopCategory(savedTopCategory);
	}

	const savedTopSubcategory = localStorage.getItem(lastTopSubcategoryKey);
	if (savedTopSubcategory) {
		setLastTopSubcategory(savedTopSubcategory);
	}

	topCategoryLinks.forEach((link) => {
		link.addEventListener("click", () => {
			const targetUrl = `${link.pathname}${link.search}`;
			localStorage.setItem(lastTopCategoryKey, targetUrl);
			localStorage.removeItem(lastTopSubcategoryKey);
			setLastTopCategory(targetUrl);
			setLastTopSubcategory("");
		});
	});

	topSubcategoryLinks.forEach((subcategoryLink) => {
		subcategoryLink.addEventListener("click", () => {
			const subcategoryTargetUrl = `${subcategoryLink.pathname}${subcategoryLink.search}`;
			localStorage.setItem(lastTopSubcategoryKey, subcategoryTargetUrl);
			setLastTopSubcategory(subcategoryTargetUrl);

			const dropdown = subcategoryLink.closest(".top-category-dropdown");
			const topCategoryLink = dropdown?.querySelector(".top-category-link");
			if (!topCategoryLink) {
				return;
			}

			const targetUrl = `${topCategoryLink.pathname}${topCategoryLink.search}`;
			localStorage.setItem(lastTopCategoryKey, targetUrl);
			setLastTopCategory(targetUrl);
		});
	});

	topCategoryDropdowns.forEach((dropdownElement) => {
		let hideTimeoutId;

		const openMenu = () => {
			if (hideTimeoutId) {
				clearTimeout(hideTimeoutId);
				hideTimeoutId = undefined;
			}
			dropdownElement.classList.add("is-open");
		};

		const closeMenu = () => {
			hideTimeoutId = setTimeout(() => {
				dropdownElement.classList.remove("is-open");
			}, 120);
		};

		dropdownElement.addEventListener("mouseenter", openMenu);
		dropdownElement.addEventListener("mouseleave", closeMenu);
		dropdownElement.addEventListener("focusin", openMenu);
		dropdownElement.addEventListener("focusout", (event) => {
			if (dropdownElement.contains(event.relatedTarget)) {
				return;
			}
			closeMenu();
		});
	});
})();
