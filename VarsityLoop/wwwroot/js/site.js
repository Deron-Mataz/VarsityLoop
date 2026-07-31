// Varsity Loop - global site scripts.
// Kept intentionally minimal (vanilla JS only, no heavy frameworks per project spec).

document.addEventListener("DOMContentLoaded", function () {
    initListingFormModuleToggle();
    initSpecificationRows();
});

// Shows/hides category-specific field groups (Book fields vs Electronics/
// Fashion/StudySupplies fields) based on the selected category's Module,
// read from a data-module attribute on each <option>. One listing form
// serves every module this way, instead of a separate form per category.
function initListingFormModuleToggle() {
    var select = document.getElementById("categorySelect");
    if (!select) return;

    var groups = document.querySelectorAll(".vl-module-fields");
    var typeInput = document.getElementById("typeInput");

    var datalistByModule = {
        Electronics: "typeSuggestionsElectronics",
        Fashion: "typeSuggestionsFashion"
    };

    function applyVisibility() {
        var selectedOption = select.options[select.selectedIndex];
        var currentModule = selectedOption ? selectedOption.getAttribute("data-module") : null;

        groups.forEach(function (group) {
            var modules = (group.getAttribute("data-for-module") || "").split(" ");
            var matches = currentModule && modules.indexOf(currentModule) !== -1;
            group.style.display = matches ? "" : "none";
        });

        if (typeInput && currentModule && datalistByModule[currentModule]) {
            typeInput.setAttribute("list", datalistByModule[currentModule]);
        }
    }

    select.addEventListener("change", applyVisibility);
    applyVisibility();
}

// Lets sellers add/remove free-text specification lines (e.g. "8GB RAM") on
// Electronics/Fashion/Study Supplies listings. Every input keeps the same
// name="Specifications" so ASP.NET Core's default model binder collects them
// into a List<string> with no extra indexing logic needed.
function initSpecificationRows() {
    var container = document.getElementById("specRows");
    var addButton = document.getElementById("addSpecBtn");
    if (!container || !addButton) return;

    addButton.addEventListener("click", function () {
        var row = document.createElement("div");
        row.className = "input-group mb-2 vl-spec-row";
        row.innerHTML =
            '<input type="text" name="Specifications" class="form-control" placeholder="e.g. 8GB RAM" />' +
            '<button type="button" class="btn btn-outline-secondary vl-spec-remove">&times;</button>';
        container.appendChild(row);
    });

    container.addEventListener("click", function (event) {
        if (event.target.classList.contains("vl-spec-remove")) {
            var rows = container.querySelectorAll(".vl-spec-row");
            if (rows.length > 1) {
                event.target.closest(".vl-spec-row").remove();
            } else {
                // Keep at least one row - just clear it instead of removing.
                event.target.closest(".vl-spec-row").querySelector("input").value = "";
            }
        }
    });
}
