// ── Sidebar toggle ────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    var toggle = document.getElementById('sidebarToggle');
    var sidebar = document.getElementById('sidebar');
    if (toggle && sidebar) {
        toggle.addEventListener('click', function () {
            sidebar.classList.toggle('open');
        });
        document.addEventListener('click', function (e) {
            if (!sidebar.contains(e.target) && !toggle.contains(e.target))
                sidebar.classList.remove('open');
        });
    }

    // ── Avatar dropdown ──────────────────────────────────────
    var avatarBtn = document.getElementById('avatarBtn');
    var avatarDrop = document.getElementById('avatarDropdown');
    if (avatarBtn && avatarDrop) {
        avatarBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            avatarDrop.classList.toggle('open');
        });
        document.addEventListener('click', function () {
            avatarDrop.classList.remove('open');
        });
    }

    // ── Tab panels ───────────────────────────────────────────
    var tabs = document.querySelectorAll('.uc-tab');
    tabs.forEach(function (tab) {
        tab.addEventListener('click', function () {
            var target = this.dataset.tab;
            document.querySelectorAll('.uc-tab').forEach(function (t) { t.classList.remove('active'); });
            document.querySelectorAll('.uc-tab-panel').forEach(function (p) { p.classList.remove('active'); });
            this.classList.add('active');
            var panel = document.getElementById('tab-' + target);
            if (panel) panel.classList.add('active');
        });
    });

    // ── Color chip picker ────────────────────────────────────
    var chips = document.querySelectorAll('.uc-color-chip');
    chips.forEach(function (chip) {
        chip.addEventListener('click', function () {
            chips.forEach(function (c) { c.classList.remove('selected'); });
            this.classList.add('selected');
            var input = document.getElementById('ColorTheme');
            if (input) input.value = this.dataset.color;
        });
    });

    // ── Live notification badge ──────────────────────────────
    var badge = document.getElementById('notifCount');
    if (badge) {
        fetch('/Notifications/UnreadCount')
            .then(function (r) { return r.json(); })
            .then(function (count) {
                if (count > 0) {
                    badge.textContent = count > 99 ? '99+' : count;
                    badge.style.display = 'flex';
                }
            })
            .catch(function () {});
    }

    // ── Auto-dismiss alerts ──────────────────────────────────
    var alerts = document.querySelectorAll('.uc-alert');
    alerts.forEach(function (a) {
        setTimeout(function () {
            a.style.transition = 'opacity .5s';
            a.style.opacity = '0';
            setTimeout(function () { a.remove(); }, 500);
        }, 4000);
    });
});

