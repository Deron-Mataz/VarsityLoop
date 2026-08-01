// Varsity Loop - global site scripts.
// Kept intentionally minimal (vanilla JS only, no heavy frameworks per project spec).

document.addEventListener("DOMContentLoaded", function () {
    initListingFormModuleToggle();
    initSpecificationRows();
    initCategoryIconPicker();
    initMarketplaceBrowse();
});

// Per-module example text for the listing form's dynamic fields (Section 2
// Pre-Phase-10 stabilization item 2). Books' Title/Author/Course placeholders
// are static in the view since that group only ever shows for Books; these
// cover the shared Type/Brand/Model/Colour/Size fields that swap meaning
// depending on which module is selected.
var LISTING_PLACEHOLDERS = {
    Electronics: { type: "e.g. Phone, Laptop, Fridge", brand: "e.g. Samsung", model: "e.g. Galaxy A06", spec: "e.g. 3GB RAM" },
    Fashion: { type: "e.g. Hoodie", brand: "e.g. Nike", model: "e.g. Tech Fleece", colour: "e.g. Black", size: "e.g. Medium" },
    Accessories: { type: "e.g. Phone Case, Sunglasses, Watch", brand: "e.g. Apple", model: "e.g. Silicone Case", colour: "e.g. Black", size: "e.g. Fits All, 42mm, Medium" },
    StudySupplies: { type: "e.g. Scientific Calculator", brand: "e.g. Casio", model: "e.g. FX-991ES Plus", spec: "e.g. Solar Powered" }
};

var TYPE_DATALIST_BY_MODULE = {
    Electronics: "typeSuggestionsElectronics",
    Fashion: "typeSuggestionsFashion",
    Accessories: "typeSuggestionsAccessories",
    StudySupplies: "typeSuggestionsStudySupplies"
};

var MODEL_OPTIONAL_MODULES = ["Fashion", "Accessories"];

// Shows/hides category-specific field groups (Book fields vs Electronics/
// Fashion/Accessories/StudySupplies fields, and the Colour+Size vs
// Specifications sub-groups within that) based on the selected category's
// Module, read from a data-module attribute on each <option>. Also swaps
// placeholder text and the Type field's datalist to match. One listing form
// serves every module this way, instead of a separate form per category.
function initListingFormModuleToggle() {
    var select = document.getElementById("CategoryId");
    if (!select) return;

    var groups = document.querySelectorAll(".vl-module-fields");
    var typeInput = document.getElementById("Type");
    var brandInput = document.getElementById("Brand");
    var modelInput = document.getElementById("ProductModel");
    var modelLabel = document.getElementById("modelLabel");
    var colourInput = document.getElementById("Colour");
    var sizeInput = document.getElementById("Size");
    var specInputs = function () { return document.querySelectorAll(".vl-spec-input"); };

    function applyVisibility() {
        var selectedOption = select.options[select.selectedIndex];
        var currentModule = selectedOption ? selectedOption.getAttribute("data-module") : null;

        groups.forEach(function (group) {
            var modules = (group.getAttribute("data-for-module") || "").split(" ");
            var matches = currentModule && modules.indexOf(currentModule) !== -1;
            group.style.display = matches ? "" : "none";
        });

        var placeholders = currentModule ? LISTING_PLACEHOLDERS[currentModule] : null;

        if (typeInput) {
            typeInput.placeholder = (placeholders && placeholders.type) || "e.g. Laptop";
            var datalistId = currentModule && TYPE_DATALIST_BY_MODULE[currentModule];
            if (datalistId) typeInput.setAttribute("list", datalistId);
        }
        if (brandInput) brandInput.placeholder = (placeholders && placeholders.brand) || "e.g. Samsung";
        if (modelInput) modelInput.placeholder = (placeholders && placeholders.model) || "e.g. Galaxy A06";
        if (colourInput && placeholders && placeholders.colour) colourInput.placeholder = placeholders.colour;
        if (sizeInput && placeholders && placeholders.size) sizeInput.placeholder = placeholders.size;
        if (modelLabel) {
            var isOptional = currentModule && MODEL_OPTIONAL_MODULES.indexOf(currentModule) !== -1;
            modelLabel.innerHTML = "Model" + (isOptional ? ' <span class="text-muted small">(Optional)</span>' : "");
        }

        var specPlaceholder = (placeholders && placeholders.spec) || "e.g. 3GB RAM";
        specInputs().forEach(function (input) { input.placeholder = specPlaceholder; });
    }

    select.addEventListener("change", applyVisibility);
    applyVisibility();
}

// Lets sellers add/remove free-text specification lines (e.g. "3GB RAM") on
// Electronics/Study Supplies listings. Inputs are named "Specifications[N]"
// with N kept contiguous (0, 1, 2, ...) on every add/remove - ASP.NET Core's
// default collection model binder stops at the first missing index, so a
// gap (e.g. deleting row 1 out of 0,1,2) would silently drop everything
// after it. renumberSpecRows() re-indexes after every change to prevent that.
function initSpecificationRows() {
    var container = document.getElementById("specRows");
    var addButton = document.getElementById("addSpecBtn");
    if (!container || !addButton) return;

    function renumberSpecRows() {
        container.querySelectorAll(".vl-spec-input").forEach(function (input, index) {
            input.setAttribute("name", "Specifications[" + index + "]");
        });
    }

    addButton.addEventListener("click", function () {
        var row = document.createElement("div");
        row.className = "input-group mb-2 vl-spec-row";
        row.innerHTML =
            '<input type="text" class="form-control vl-spec-input" placeholder="e.g. 3GB RAM" />' +
            '<button type="button" class="btn btn-outline-secondary vl-spec-remove">&times;</button>';
        container.appendChild(row);
        renumberSpecRows();
    });

    container.addEventListener("click", function (event) {
        if (event.target.classList.contains("vl-spec-remove")) {
            var rows = container.querySelectorAll(".vl-spec-row");
            if (rows.length > 1) {
                event.target.closest(".vl-spec-row").remove();
                renumberSpecRows();
            } else {
                // Keep at least one row - just clear it instead of removing.
                event.target.closest(".vl-spec-row").querySelector("input").value = "";
            }
        }
    });

    renumberSpecRows();
}

// Filters the category-icon picker (Admin > Categories) down to only the
// icons suggested for the currently selected Module - see
// Views/AdminCategories/_CategoryIconPicker.cshtml.
function initCategoryIconPicker() {
    var moduleSelect = document.getElementById("Module");
    var iconGroups = document.querySelectorAll(".vl-icon-group");
    if (!moduleSelect || iconGroups.length === 0) return;

    function applyVisibility() {
        var currentModule = moduleSelect.value;
        iconGroups.forEach(function (group) {
            group.style.display = group.getAttribute("data-icon-module") === currentModule ? "" : "none";
        });
    }

    moduleSelect.addEventListener("change", applyVisibility);
    applyVisibility();
}

// Drives the Marketplace browsing experience: module button row, category
// chip row (populated per-module client-side from data already on the page),
// and AJAX-refreshed results so switching module/category/search doesn't
// reload the page. Falls back to a normal full-page form submit if fetch
// fails for any reason - the page still works, it just reloads.
function initMarketplaceBrowse() {
    var root = document.getElementById("marketplaceRoot");
    if (!root) return;

    var resultsContainer = document.getElementById("marketplaceResults");
    var moduleButtons = root.querySelectorAll(".vl-module-btn");
    var categoryChipsContainer = document.getElementById("categoryChips");
    var searchInput = document.getElementById("marketplaceSearchInput");
    var searchForm = document.getElementById("marketplaceSearchForm");
    var baseUrl = root.getAttribute("data-browse-url");

    var state = {
        module: root.getAttribute("data-initial-module") || "",
        categoryId: root.getAttribute("data-initial-category") || "",
        q: (searchInput && searchInput.value) || "",
        sort: root.getAttribute("data-initial-sort") || "Newest",
        page: 1
    };

    function buildUrl(pushHistory) {
        var params = new URLSearchParams();
        if (state.q) params.set("q", state.q);
        if (state.module) params.set("module", state.module);
        if (state.categoryId) params.set("categoryId", state.categoryId);
        if (state.sort) params.set("sort", state.sort);
        if (state.page > 1) params.set("page", state.page);
        var url = baseUrl + (params.toString() ? "?" + params.toString() : "");
        if (pushHistory !== false) {
            window.history.pushState({ vlMarketplace: state }, "", url);
        }
        return url;
    }

    function renderCategoryChips() {
        if (!categoryChipsContainer) return;
        var allChips = categoryChipsContainer.querySelectorAll(".vl-category-chip");
        var anyVisible = false;
        allChips.forEach(function (chip) {
            var matches = state.module && chip.getAttribute("data-module") === state.module;
            chip.style.display = matches ? "" : "none";
            if (matches) anyVisible = true;
        });
        categoryChipsContainer.style.display = state.module && anyVisible ? "" : "none";
    }

    function setActiveModuleButton() {
        moduleButtons.forEach(function (btn) {
            var isActive = (btn.getAttribute("data-module") || "") === state.module;
            btn.classList.toggle("active", isActive);
        });
    }

    function setActiveCategoryChip() {
        if (!categoryChipsContainer) return;
        categoryChipsContainer.querySelectorAll(".vl-category-chip").forEach(function (chip) {
            chip.classList.toggle("active", chip.getAttribute("data-category-id") === state.categoryId);
        });
    }

    function loadResults(pushHistory) {
        var url = buildUrl(pushHistory);
        var fetchUrl = url + (url.indexOf("?") === -1 ? "?" : "&") + "partial=1";

        resultsContainer.style.opacity = "0.5";

        fetch(fetchUrl, { headers: { "X-Requested-With": "XMLHttpRequest" } })
            .then(function (response) {
                if (!response.ok) throw new Error("Request failed");
                return response.text();
            })
            .then(function (html) {
                resultsContainer.innerHTML = html;
                resultsContainer.style.opacity = "1";
                attachResultsPaginationHandlers();
            })
            .catch(function () {
                // Fall back to a real navigation rather than leaving the user
                // stuck on a half-updated page.
                window.location.href = url;
            });
    }

    function attachResultsPaginationHandlers() {
        resultsContainer.querySelectorAll("[data-marketplace-page]").forEach(function (link) {
            link.addEventListener("click", function (event) {
                event.preventDefault();
                state.page = parseInt(link.getAttribute("data-marketplace-page"), 10) || 1;
                loadResults(true);
                resultsContainer.scrollIntoView({ behavior: "smooth", block: "start" });
            });
        });
    }

    moduleButtons.forEach(function (btn) {
        btn.addEventListener("click", function () {
            state.module = btn.getAttribute("data-module") || "";
            state.categoryId = "";
            state.page = 1;
            setActiveModuleButton();
            renderCategoryChips();
            setActiveCategoryChip();
            loadResults(true);
        });
    });

    if (categoryChipsContainer) {
        categoryChipsContainer.querySelectorAll(".vl-category-chip").forEach(function (chip) {
            chip.addEventListener("click", function () {
                var clickedId = chip.getAttribute("data-category-id");
                state.categoryId = state.categoryId === clickedId ? "" : clickedId;
                state.page = 1;
                setActiveCategoryChip();
                loadResults(true);
            });
        });
    }

    if (searchForm) {
        searchForm.addEventListener("submit", function (event) {
            event.preventDefault();
            state.q = searchInput ? searchInput.value : "";
            state.page = 1;
            loadResults(true);
        });
    }

    window.addEventListener("popstate", function (event) {
        if (event.state && event.state.vlMarketplace) {
            state = event.state.vlMarketplace;
            setActiveModuleButton();
            renderCategoryChips();
            setActiveCategoryChip();
            loadResults(false);
        }
    });

    renderCategoryChips();
    setActiveCategoryChip();
    attachResultsPaginationHandlers();
}
