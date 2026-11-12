// The app shell: the side menu, its drawer on a phone, and "find a screen".
// Everything a single page needs lives in that page's own Scripts section.
(function () {
    'use strict';

    var root = document.documentElement;
    var side = document.getElementById('zxSide');
    if (!side) { return; }   // signed out: there is no menu

    var toggle = document.getElementById('zxNavToggle');
    var backdrop = document.getElementById('zxBackdrop');
    var find = document.getElementById('zxNavFind');
    var none = document.getElementById('zxNavNone');
    var desktop = window.matchMedia('(min-width: 992px)');

    // ---- open / close -------------------------------------------------
    // One button, two jobs. On a desktop the menu is always on screen and
    // the button folds it down to icons, remembered for next time. On a
    // phone the menu is a drawer and the button opens it.
    function setDrawer(open) {
        root.classList.toggle('zx-nav-open', open);
        if (toggle) { toggle.setAttribute('aria-expanded', open ? 'true' : 'false'); }
    }

    function setRail(on) {
        root.classList.toggle('zx-rail', on);
        try { localStorage.setItem('zx-nav-rail', on ? '1' : '0'); } catch (e) { }
    }

    if (toggle) {
        toggle.addEventListener('click', function () {
            if (desktop.matches) {
                setRail(!root.classList.contains('zx-rail'));
            } else {
                setDrawer(!root.classList.contains('zx-nav-open'));
            }
        });
    }

    if (backdrop) { backdrop.addEventListener('click', function () { setDrawer(false); }); }

    // A drawer left open would still be open, over nothing, after the
    // window is widened past the breakpoint.
    var onResize = function () { if (desktop.matches) { setDrawer(false); } };
    if (desktop.addEventListener) { desktop.addEventListener('change', onResize); }
    else if (desktop.addListener) { desktop.addListener(onResize); }

    // Keep the lit link in view when the menu is longer than the screen.
    var current = side.querySelector('.zx-side-link.is-active');
    if (current && current.scrollIntoView) { current.scrollIntoView({ block: 'nearest' }); }

    // ---- find a screen --------------------------------------------------
    function links() {
        return Array.prototype.slice.call(side.querySelectorAll('.zx-side-link'));
    }

    function applyFilter() {
        var term = find.value.trim().toLowerCase();
        var any = false;

        links().forEach(function (link) {
            var match = term === '' || (link.getAttribute('data-nav-name') || '').indexOf(term) !== -1;
            link.hidden = !match;
            if (match) { any = true; }
        });

        // A group with nothing left in it should not leave its name behind.
        Array.prototype.forEach.call(side.querySelectorAll('.zx-side-group'), function (group) {
            group.hidden = !group.querySelector('.zx-side-link:not([hidden])');
        });

        if (none) { none.hidden = any; }
    }

    function openFind() {
        if (desktop.matches) { setRail(false); } else { setDrawer(true); }
        find.focus();
        find.select();
    }

    if (find) {
        find.addEventListener('input', applyFilter);

        find.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                // Enter goes to the first screen still showing.
                var first = side.querySelector('.zx-side-link:not([hidden])');
                if (first && find.value.trim() !== '') { e.preventDefault(); window.location.href = first.href; }
            } else if (e.key === 'ArrowDown') {
                var next = side.querySelector('.zx-side-link:not([hidden])');
                if (next) { e.preventDefault(); next.focus(); }
            } else if (e.key === 'Escape' && find.value !== '') {
                e.stopPropagation();
                find.value = '';
                applyFilter();
            }
        });

        // Up and down move through the links, the way a menu should.
        side.addEventListener('keydown', function (e) {
            if (e.key !== 'ArrowDown' && e.key !== 'ArrowUp') { return; }
            var shown = links().filter(function (l) { return !l.hidden; });
            var at = shown.indexOf(document.activeElement);
            if (at === -1) { return; }
            e.preventDefault();
            var to = at + (e.key === 'ArrowDown' ? 1 : -1);
            if (to < 0) { find.focus(); } else if (to < shown.length) { shown[to].focus(); }
        });
    }

    document.addEventListener('keydown', function (e) {
        if (find && (e.ctrlKey || e.metaKey) && !e.altKey && !e.shiftKey && (e.key === 'k' || e.key === 'K')) {
            e.preventDefault();
            openFind();
        } else if (e.key === 'Escape' && root.classList.contains('zx-nav-open')) {
            setDrawer(false);
            if (toggle) { toggle.focus(); }
        }
    });
})();
