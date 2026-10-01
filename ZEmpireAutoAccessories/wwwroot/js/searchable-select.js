/* Searchable select: progressively enhances every <select> on the page into
 * a dropdown you can also type in, without changing how the form submits or
 * how ASP.NET's unobtrusive validation works. Focusing or clicking shows the
 * whole list at once, exactly like a plain <select>; typing narrows it.
 * Nothing has to be typed to reach the options, so a short list behaves the
 * same as it always did. Put data-searchable="false" on a select to leave it
 * as a native control.
 *
 * The original <select> is kept in the DOM (visually hidden, not
 * display:none, so jQuery Validate's default `ignore: ":hidden"` doesn't
 * skip it) and stays the single source of truth: selecting an option here
 * sets select.value and fires a native "change" event, so any existing
 * $('#CustomerID').on('change', ...) handler (e.g. the Customer -> Vehicle
 * cascading dropdowns) keeps working unmodified.
 *
 * Works with selects added to the page after load (e.g. a cloned line-item
 * row) and with selects whose <option> list or disabled state is changed
 * programmatically after init (e.g. the Vehicle dropdown repopulated via
 * AJAX once a Customer is picked).
 */
(function () {
    "use strict";

    var idCounter = 0;

    function nextId(prefix) {
        idCounter += 1;
        return prefix + "-" + idCounter;
    }

    function optionDisplayText(option) {
        var text = option.textContent || "";
        return text.trim();
    }

    function initOne(select) {
        if (!select || select.dataset.ssInit === "true") {
            return;
        }
        select.dataset.ssInit = "true";

        var wrap = document.createElement("div");
        wrap.className = "ss-wrap";
        select.parentNode.insertBefore(wrap, select);

        var input = document.createElement("input");
        input.type = "text";
        // Only take layout/sizing classes from the select (e.g. "form-select-sm"),
        // not functional hook classes like "product-select" - those are meant to
        // target the real <select> (e.g. $(document).on('change', '.product-select', ...)),
        // not this decorative stand-in, or jQuery selectors elsewhere would double-match.
        var sizeClasses = (select.className.match(/\bform-select-\S+/g) || []).join(" ");
        input.className = ("form-control ss-input " + sizeClasses).trim();
        input.autocomplete = "off";
        input.setAttribute("role", "combobox");
        input.setAttribute("aria-autocomplete", "list");
        input.setAttribute("aria-expanded", "false");
        if (select.disabled) {
            input.disabled = true;
        }

        var menu = document.createElement("div");
        menu.className = "ss-menu";
        menu.setAttribute("role", "listbox");
        menu.hidden = true;
        menu.id = nextId("ss-menu");
        input.setAttribute("aria-controls", menu.id);

        var empty = document.createElement("div");
        empty.className = "ss-empty";
        empty.textContent = "No matches found";
        empty.hidden = true;
        menu.appendChild(empty);

        wrap.appendChild(input);
        wrap.appendChild(menu);
        wrap.appendChild(select);
        select.classList.add("ss-native-hidden");
        select.tabIndex = -1;
        select.setAttribute("aria-hidden", "true");

        var activeIndex = -1;

        function optionRows() {
            return Array.prototype.slice.call(menu.querySelectorAll(".ss-option"));
        }

        function visibleOptionRows() {
            return optionRows().filter(function (row) { return !row.hidden; });
        }

        function rebuildMenu() {
            optionRows().forEach(function (row) { row.remove(); });

            Array.prototype.forEach.call(select.options, function (option, index) {
                var row = document.createElement("div");
                row.className = "ss-option";
                row.setAttribute("role", "option");
                row.dataset.value = option.value;
                row.dataset.index = String(index);
                row.textContent = optionDisplayText(option);
                if (option.value === "") {
                    row.classList.add("ss-placeholder");
                }
                menu.insertBefore(row, empty);
            });
        }

        // Text for the blank/placeholder option (e.g. "-- Select Customer --"),
        // if the select has one. Shown via the input's placeholder attribute,
        // never written into its value - see currentOptionText below.
        function blankOptionText() {
            var blank = Array.prototype.filter.call(select.options, function (o) {
                return o.value === "";
            })[0];
            return blank ? optionDisplayText(blank) : "";
        }

        // Empty when nothing is really selected (select.value === ""), even
        // though a placeholder option with that value may have its own text.
        // Keeping this in sync with select.value is what makes the native
        // "required" constraint on the input agree with the real select.
        function currentOptionText() {
            if (select.value === "") { return ""; }
            var selected = select.options[select.selectedIndex];
            return selected ? optionDisplayText(selected) : "";
        }

        function syncFromSelect() {
            rebuildMenu();
            input.placeholder = blankOptionText();
            input.value = currentOptionText();
            input.disabled = select.disabled;
            // Selects without ASP.NET's unobtrusive validation (data-val
            // attributes) rely on the native "required" constraint instead;
            // mirroring it here means the browser's validation bubble on
            // submit anchors to this visible input (first in DOM order)
            // rather than the real select, which is now visually hidden.
            input.required = select.required;
            markSelected();
        }

        function markSelected() {
            optionRows().forEach(function (row) {
                row.classList.toggle("ss-selected", row.dataset.value === select.value);
            });
        }

        function setActive(index) {
            var rows = visibleOptionRows();
            optionRows().forEach(function (row) { row.classList.remove("ss-active"); });
            activeIndex = index;
            if (index >= 0 && index < rows.length) {
                rows[index].classList.add("ss-active");
                rows[index].scrollIntoView({ block: "nearest" });
            }
        }

        function filter(term) {
            var needle = term.trim().toLowerCase();
            var rows = optionRows();
            var anyVisible = false;
            rows.forEach(function (row) {
                var match = needle === "" || row.textContent.toLowerCase().indexOf(needle) !== -1;
                row.hidden = !match;
                if (match) { anyVisible = true; }
            });
            empty.hidden = anyVisible;
            setActive(anyVisible ? 0 : -1);
        }

        var MENU_MAX_HEIGHT = 260;
        var MENU_GAP = 4;

        // The menu is position:fixed, so it has to be told where to sit.
        // Below the input by default; above it when there is not enough room
        // below and more room above - otherwise a row near the bottom of the
        // window would open into a sliver.
        function positionMenu() {
            var rect = input.getBoundingClientRect();
            var below = window.innerHeight - rect.bottom - MENU_GAP;
            var above = rect.top - MENU_GAP;
            var flip = below < Math.min(MENU_MAX_HEIGHT, 160) && above > below;

            menu.style.left = rect.left + "px";
            menu.style.width = rect.width + "px";

            if (flip) {
                menu.style.top = "auto";
                menu.style.bottom = (window.innerHeight - rect.top + MENU_GAP) + "px";
                menu.style.maxHeight = Math.max(80, Math.min(MENU_MAX_HEIGHT, above)) + "px";
            } else {
                menu.style.bottom = "auto";
                menu.style.top = (rect.bottom + MENU_GAP) + "px";
                menu.style.maxHeight = Math.max(80, Math.min(MENU_MAX_HEIGHT, below)) + "px";
            }
        }

        // Capture phase, so scrolling any container the input sits in counts,
        // not just the window.
        function onViewportChange() {
            if (!menu.hidden) { positionMenu(); }
        }

        // showAll ignores whatever is sitting in the box, which is the
        // current selection's own text - opening on focus should offer the
        // whole list, not just the option already chosen.
        function openMenu(showAll) {
            if (select.disabled) { return; }
            menu.hidden = false;
            input.setAttribute("aria-expanded", "true");
            // Place it before filtering: filter() highlights the first match
            // and scrolls it into view, which needs the real geometry.
            positionMenu();
            filter(showAll ? "" : input.value);
            window.addEventListener("scroll", onViewportChange, true);
            window.addEventListener("resize", onViewportChange);
        }

        function closeMenu(revert) {
            menu.hidden = true;
            input.setAttribute("aria-expanded", "false");
            window.removeEventListener("scroll", onViewportChange, true);
            window.removeEventListener("resize", onViewportChange);
            setActive(-1);
            if (revert) {
                input.value = currentOptionText();
            }
        }

        // Tell everybody the value moved.
        //
        // This used to be $(select).trigger("change") alone, which only wakes
        // handlers bound through jQuery. A plain
        // select.addEventListener("change", ...) never heard it - and that is
        // how line-unit.js listens, so picking film through one of these boxes
        // left the unit list on pieces and the stock note blank. Dispatching a
        // real DOM event reaches both: jQuery binds natively underneath, so
        // its handlers still run, and anything listening the plain way finally
        // does too.
        function notifyChanged(element) {
            element.dispatchEvent(new Event("change", { bubbles: true }));
        }

        function commit(row) {
            if (!row) { return; }
            select.value = row.dataset.value;
            input.value = currentOptionText();
            markSelected();
            closeMenu(false);
            notifyChanged(select);
        }

        // Dropdown first, search second: focusing or clicking shows the whole
        // list straight away, and typing narrows it. Nothing has to be typed
        // to get at the options. The text is selected on focus so the first
        // keystroke replaces the current choice rather than appending to it.
        input.addEventListener("focus", function () {
            input.select();
            openMenu(true);
        });

        // Clicking a field that already has focus reopens a menu that was
        // dismissed with Escape or by picking something.
        input.addEventListener("click", function () {
            if (menu.hidden) { openMenu(true); }
        });

        // An empty box means "no filter", so the full list stays up.
        input.addEventListener("input", function () {
            openMenu(false);
        });

        input.addEventListener("keydown", function (e) {
            var rows;
            if (e.key === "ArrowDown") {
                // Same as a native select: Down opens a closed list.
                if (menu.hidden) { e.preventDefault(); openMenu(true); return; }
                e.preventDefault();
                rows = visibleOptionRows();
                setActive(Math.min(activeIndex + 1, rows.length - 1));
            } else if (e.key === "ArrowUp") {
                if (menu.hidden) { return; }
                e.preventDefault();
                setActive(Math.max(activeIndex - 1, 0));
            } else if (e.key === "Enter") {
                if (!menu.hidden) {
                    e.preventDefault();
                    rows = visibleOptionRows();
                    commit(rows[activeIndex] || rows[0]);
                }
            } else if (e.key === "Escape") {
                if (!menu.hidden) {
                    e.preventDefault();
                    closeMenu(true);
                }
            } else if (e.key === "Tab") {
                closeMenu(true);
            }
        });

        input.addEventListener("blur", function () {
            // Let a click on a menu row register before we close/revert.
            setTimeout(function () {
                if (!wrap.contains(document.activeElement)) {
                    closeMenu(true);
                }
            }, 150);
        });

        menu.addEventListener("mousedown", function (e) {
            var row = e.target.closest(".ss-option");
            if (row) {
                e.preventDefault();
                commit(row);
            }
        });

        document.addEventListener("click", function (e) {
            if (!wrap.contains(e.target)) {
                closeMenu(true);
            }
        });

        var observer = new MutationObserver(function () {
            syncFromSelect();
            input.classList.toggle("ss-invalid", select.classList.contains("input-validation-error"));
        });
        observer.observe(select, { childList: true, attributes: true, attributeFilter: ["disabled", "class", "required"] });

        // Picking an option here already updates the display directly (see
        // commit above), but other code sets select.value programmatically
        // and fires its own "change" (e.g. auto-selecting a customer's only
        // vehicle) - a plain value change isn't a DOM mutation the observer
        // above would catch, so re-sync on "change" too.
        select.addEventListener("change", syncFromSelect);

        syncFromSelect();
    }

    // Every <select> is enhanced. Opt a single one back out to a native
    // control with data-searchable="false". Multi-selects and list boxes
    // are skipped outright - this widget only knows how to hold one value.
    var SELECTOR = 'select:not([data-searchable="false"]):not([multiple]):not([size])';

    function scan(root) {
        if (!root || !root.querySelectorAll) { return; }
        if (root.matches && root.matches(SELECTOR)) {
            initOne(root);
        }
        root.querySelectorAll(SELECTOR).forEach(initOne);
    }

    document.addEventListener("DOMContentLoaded", function () {
        scan(document);

        var bodyObserver = new MutationObserver(function (mutations) {
            mutations.forEach(function (m) {
                m.addedNodes.forEach(function (node) {
                    if (node.nodeType === 1) { scan(node); }
                });
            });
        });
        bodyObserver.observe(document.body, { childList: true, subtree: true });
    });
})();
