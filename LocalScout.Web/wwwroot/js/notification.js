"use strict";

(function () {
    let notifications = [];
    const POLLING_INTERVAL = 30000; // 30 seconds

    // Initialize on page load
    $(document).ready(function () {
        loadUnreadCount();

        // Start polling
        setInterval(loadUnreadCount, POLLING_INTERVAL);

        // Bind events
        bindEvents();
    });

    function bindEvents() {
        // Bell icon click - handled by Bootstrap Modal data-toggle, 
        // but we want to load notifications when it opens.
        $('#notificationListModal').on('show.bs.modal', function () {
            loadNotifications();
        });

        // Mark All Read button
        $('#markAllReadBtn').on('click', function (e) {
            e.preventDefault();
            markAllAsRead();
        });
    }

    // Load unread count for the badge
    function loadUnreadCount() {
        $.ajax({
            url: '/api/Notification/count',
            type: 'GET',
            success: function (response) {
                updateBadge(response.count);
            },
            error: function () {
                console.error('Failed to load notification count');
            }
        });
    }

    // Load full notification list
    function loadNotifications() {
        const listContainer = $('#notificationListContainer');
        listContainer.html(`
            <div class="text-center py-5">
                <div class="spinner-border text-primary" role="status">
                    <span class="sr-only">Loading...</span>
                </div>
            </div>
        `);

        $.ajax({
            url: '/api/Notification/list',
            type: 'GET',
            data: { take: 50 },
            success: function (data) {
                notifications = data;
                renderNotifications(data);
            },
            error: function () {
                listContainer.html(`
                    <div class="text-center text-danger py-5">
                        <p>Failed to load notifications.</p>
                        <button class="btn btn-sm btn-outline-primary" onclick="window.notificationPanel.reload()">Retry</button>
                    </div>
                `);
            }
        });
    }

    // Update the bell badge
    function updateBadge(count) {
        const badge = $('#notificationBadge');

        if (count > 0) {
            badge.text(count > 99 ? '99+' : count);
            badge.show();
        } else {
            badge.hide();
        }
    }

    // Render the list inside the modal
    function renderNotifications(items) {
        const listContainer = $('#notificationListContainer');

        if (!items || items.length === 0) {
            listContainer.html(`
                <div class="text-center text-muted py-5">
                    <i class="far fa-bell-slash fa-3x mb-3"></i>
                    <p class="mb-0">No notifications</p>
                </div>
            `);
            return;
        }

        let html = '<div class="list-group list-group-flush">';
        items.forEach(function (notification) {
            const unreadClass = !notification.isRead ? 'bg-light font-weight-bold' : '';
            const iconClass = getIconForNotification(notification);

            html += `
                <a href="#" class="list-group-item list-group-item-action ${unreadClass} notification-item" data-id="${notification.id}">
                    <div class="d-flex w-100 justify-content-between">
                        <h6 class="mb-1 text-primary">
                            ${iconClass} ${escapeHtml(notification.title)}
                        </h6>
                        <small class="text-muted">${notification.timeAgo}</small>
                    </div>
                    <p class="mb-1 text-dark small">${escapeHtml(notification.message)}</p>
                </a>
            `;
        });
        html += '</div>';

        listContainer.html(html);

        // Attach click handlers to items
        $('.notification-item').on('click', function (e) {
            e.preventDefault();
            const id = $(this).data('id');
            showNotificationDetail(id);
        });
    }

    function getIconForNotification(n) {
        // Simple heuristic for icons based on title/content
        const title = (n.title || '').toLowerCase();
        if (title.includes('approved') || title.includes('unblocked')) return '<i class="fas fa-check-circle text-success mr-2"></i>';
        if (title.includes('rejected') || title.includes('blocked')) return '<i class="fas fa-ban text-danger mr-2"></i>';
        if (title.includes('request')) return '<i class="fas fa-user-plus text-info mr-2"></i>';
        return '<i class="fas fa-info-circle text-primary mr-2"></i>';
    }

    // Format metaJson into user-friendly text
    function formatMetaInfo(metaJson) {
        if (!metaJson) return '';

        try {
            const meta = JSON.parse(metaJson);
            let html = '<div class="mt-3">';

            // Handle common meta fields
            if (meta.reason) {
                html += `
                    <div class="alert alert-warning mb-0">
                        <strong><i class="fas fa-exclamation-triangle mr-1"></i> Reason:</strong>
                        <p class="mb-0 mt-1">${escapeHtml(meta.reason)}</p>
                    </div>
                `;
            }

            // Handle other meta fields generically
            const handledKeys = ['reason'];
            const otherKeys = Object.keys(meta).filter(k => !handledKeys.includes(k));

            if (otherKeys.length > 0) {
                html += '<div class="mt-2">';
                otherKeys.forEach(key => {
                    const label = key.charAt(0).toUpperCase() + key.slice(1).replace(/([A-Z])/g, ' $1');
                    html += `<p class="mb-1"><strong>${escapeHtml(label)}:</strong> ${escapeHtml(String(meta[key]))}</p>`;
                });
                html += '</div>';
            }

            html += '</div>';
            return html;
        } catch (e) {
            // If JSON parsing fails, return as plain text
            return `<div class="mt-3"><p class="text-muted mb-0">${escapeHtml(metaJson)}</p></div>`;
        }
    }

    // Show detail modal
    function showNotificationDetail(id) {
        const notification = notifications.find(n => n.id === id);
        if (!notification) return;

        // Hide list modal, show detail modal
        $('#notificationListModal').modal('hide');
        $('#notificationDetailModal').modal('show');

        // Populate detail
        const modalBody = $('#notificationModalBody');
        modalBody.html(`
            <div class="mb-4">
                <h5 class="text-primary mb-1">${escapeHtml(notification.title)}</h5>
                <small class="text-muted">
                    <i class="far fa-clock"></i> ${notification.timeAgo}
                </small>
            </div>
            <div class="p-3 bg-light rounded mb-3">
                <p class="mb-0 text-dark">${escapeHtml(notification.message)}</p>
            </div>
            ${formatMetaInfo(notification.metaJson)}
        `);

        // Mark as read if needed
        if (!notification.isRead) {
            markAsRead(id);
        }
    }

    function markAsRead(id) {
        $.ajax({
            url: `/api/Notification/${id}/mark-read`,
            type: 'POST',
            success: function (response) {
                // Update local state
                const n = notifications.find(x => x.id === id);
                if (n) n.isRead = true;

                // Update badge
                updateBadge(response.unreadCount);
            }
        });
    }

    function markAllAsRead() {
        $.ajax({
            url: '/api/Notification/mark-all-read',
            type: 'POST',
            success: function (response) {
                updateBadge(0);
                loadNotifications(); // Reload list to remove bold styling

                // Toast
                if (typeof Swal !== 'undefined') {
                    Swal.fire({
                        icon: 'success',
                        title: 'All notifications marked as read',
                        toast: true,
                        position: 'top-end',
                        showConfirmButton: false,
                        timer: 2000
                    });
                }
            }
        });
    }

    function escapeHtml(text) {
        if (!text) return '';
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text.replace(/[&<>"']/g, m => map[m]);
    }

    // Expose global
    window.notificationPanel = {
        reload: function () {
            loadUnreadCount();
            // If modal is open, reload list
            if ($('#notificationListModal').hasClass('show')) {
                loadNotifications();
            }
        }
    };

})();