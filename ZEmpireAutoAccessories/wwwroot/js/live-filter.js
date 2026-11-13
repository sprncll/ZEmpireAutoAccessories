// Live filtering for the list screens.
//
// Every list in this system has the same shape: a search box, a Search
// button, and a table underneath. Pressing Search asked the server which
// rows match, reloaded the page and lost the cursor - for a list of
// seventy-four rows that were already on the screen. This filters them
// where they stand, as the letters are typed.
//
// Nothing here replaces the form. The button still posts, the term still
// reaches the address bar, and a link to a search still works - which is
// also what happens for anybody with JavaScript off. This is only what
// happens before the button is pressed.
//
// By convention, so a list needs no markup of its own: the page's search
// box is input.search-input[name=q] and the list is the first table after
// it. A list whose figures would be wrong once rows are hidden - anything
// with a <tfoot> of totals - is left alone.
(function () {
    var input = document.querySelector('input.search-input[name="q"]');
    if (!input) { return; }

    var table = document.querySelector('.zx-table') || document.querySelector('table');
    if (!table || !table.tBodies.length) { return; }

    // A total under a filtered list is a wrong total.
    if (table.tFoot) { return; }

    var body = table.tBodies[0];
    var all = Array.prototype.slice.call(body.rows);

    // Band rows (a category heading inside the list) are not matched - they
    // are shown when something under them is.
    var bands = all.filter(function (row) { return row.classList.contains('cat-row'); });
    var rows = all.filter(function (row) {
        return !row.classList.contains('cat-row')
            && !row.hasAttribute('data-no-filter')
            && row.id !== 'noMatch';
    });

    if (rows.length === 0) { return; }

    var noun = input.getAttribute('data-live-noun') || 'items';

    // On a paged list only this page's rows are here to filter. Say so, and
    // point at Enter - the form's own search - which asks the server and so
    // covers every page.
    var pager = document.querySelector('.zx-pager[data-paged="true"]');
    var allCount = pager ? parseInt(pager.getAttribute('data-total'), 10) : null;
    // What the server already searched for: typing it again needs no Enter.
    var applied = input.value.trim().toLowerCase();
    var columns = (table.tHead && table.tHead.rows.length)
        ? table.tHead.rows[0].cells.length
        : 1;

    // What each row says, flattened once. Reading textContent per keystroke
    // on seventy rows is work that never changes its answer.
    var haystack = rows.map(function (row) {
        return (row.textContent || '').replace(/\s+/g, ' ').trim().toLowerCase();
    });

    // And the same row as nothing but digits, for the numbers people type
    // without their spaces: a contact number, a plate, an invoice number.
    var digits = haystack.map(function (text) { return text.replace(/\D+/g, ''); });

    // A number typed the way it is said. The shop's numbers are stored as
    // +63 917 ..., and somebody reading one off a phone types 0917 - the
    // same number with a different first digit, which matches nothing at all
    // unless it is said here.
    function numberForms(term) {
        var forms = [term];
        if (term.charAt(0) === '0') { forms.push('63' + term.slice(1)); }
        if (term.slice(0, 2) === '63') { forms.push('0' + term.slice(2)); }
        return forms;
    }

    // The count goes above the list. Inventory has its own line already -
    // use that rather than growing a second one.
    var count = document.getElementById('listCount');
    if (!count) {
        var meta = document.createElement('p');
        meta.className = 'live-meta';
        count = document.createElement('span');
        meta.appendChild(count);

        var card = table.closest('.zx-card') || table;
        card.parentNode.insertBefore(meta, card);
    }

    // And the line that says a search matched nothing, which the server
    // never hears about.
    var empty = document.getElementById('noMatch');
    if (!empty) {
        empty = document.createElement('tr');
        empty.id = 'noMatch';
        empty.hidden = true;

        var cell = document.createElement('td');
        cell.colSpan = columns;
        cell.className = 'text-center text-muted py-4';
        cell.textContent = 'Nothing matches what you typed.';

        empty.appendChild(cell);
        body.appendChild(empty);
    }

    function apply() {
        var typed = input.value.trim();

        // Every word has to appear somewhere in the row, so "reyes van"
        // finds the row that holds both rather than the rows that hold
        // either.
        var terms = typed.toLowerCase().split(/\s+/).filter(Boolean);
        var shown = 0;

        rows.forEach(function (row, i) {
            var text = haystack[i];
            var figures = digits[i];

            var match = terms.every(function (term) {
                if (text.indexOf(term) !== -1) { return true; }

                // Digits are matched against the row's digits, so "0917555"
                // finds "+63 917 555 0101" and "JO 2026" finds JO-2026-00012.
                if (!/^\d+$/.test(term)) { return false; }

                return numberForms(term).some(function (form) {
                    return figures.indexOf(form) !== -1;
                });
            });

            row.hidden = !match;
            if (match) { shown++; }
        });

        // A heading with nothing under it is a heading for an empty shelf.
        bands.forEach(function (band) {
            var key = band.getAttribute('data-cat');
            var any = false;

            for (var i = 0; i < rows.length && !any; i++) {
                if (rows[i].hidden) { continue; }
                any = !key || rows[i].getAttribute('data-cat') === key;
            }

            band.hidden = !any;
        });

        empty.hidden = shown > 0;
        if (!empty.hidden) {
            empty.cells[0].textContent = pager && typed.toLowerCase() !== applied
                ? 'Nothing on this page matches. Press Enter to search all ' + allCount + '.'
                : 'Nothing matches what you typed.';
        }

        count.textContent = terms.length === 0
            ? (pager ? rows.length + ' of ' + allCount + ' ' + noun + ' on this page' : rows.length + ' ' + noun)
            : shown + ' of ' + rows.length + (pager ? ' on this page' : '') + ' match "' + typed + '"'
              + (pager && typed.toLowerCase() !== applied ? ' - press Enter to search all ' + allCount : '');
    }

    input.addEventListener('input', apply);

    // Escape clears it, which is quicker than selecting and deleting.
    input.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && input.value) {
            e.preventDefault();
            input.value = '';
            apply();
        }
    });

    // "/" from anywhere on the page lands in the search, so hands stay on
    // the keyboard between one row and the next.
    document.addEventListener('keydown', function (e) {
        if (e.key !== '/' || e.ctrlKey || e.metaKey || e.altKey) { return; }

        var active = document.activeElement;
        if (active && /^(INPUT|SELECT|TEXTAREA)$/.test(active.tagName)) { return; }
        if (active && active.isContentEditable) { return; }

        e.preventDefault();
        input.focus();
        input.select();
    });

    // The cursor starts in the search box, because that is what a list of
    // seventy is opened to do. Not where the page has already put it
    // somewhere deliberate - a row just stocked, say.
    if (!document.querySelector('[autofocus]')
        && !(document.activeElement && document.activeElement !== document.body)) {
        input.focus();
    }

    apply();
})();
