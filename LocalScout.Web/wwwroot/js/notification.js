"use strict";

(function () {
  let notifications = [];
  let showingAll = false;
  const PREVIEW_COUNT = 5; // Show only 5 in dropdown initially
  const POLLING_INTERVAL = 30000; // 30 seconds

  // Context-aware action URL mapping based on notification title/type
  // Note: blocked/unblocked removed since users/providers don't have access to admin pages
  const ACTION_URL_MAP = {
    "verification request": "/Admin/VerificationRequests",
    "category request": "/CategoryRequest/PendingRequests",
    "new booking": "/Booking/ProviderBookings",
    "booking request": "/Booking/ProviderBookings",
    "booking confirmed": "/Booking/MyBookings",
    "booking cancelled": "/Booking/MyBookings",
    "booking completed": "/Booking/MyBookings",
    "booking accepted": "/Booking/MyBookings",
    "booking rejected": "/Booking/MyBookings",
    "payment received": "/Payment/History",
    payment: "/Payment/History",
    "new review": "/Review/ProviderReviews",
    review: "/Review/ProviderReviews",
    "provider approved": "/Provider/Index",
    "provider verified": "/Provider/Index",
    "account approved": "/Provider/Index",
  };

  // Initialize on page load
  $(document).ready(function () {
    loadUnreadCount();
    setInterval(loadUnreadCount, POLLING_INTERVAL);
    bindEvents();
  });

  function bindEvents() {
    // Dropdown shown - load notifications
    $("#notificationDropdownToggle")
      .parent()
      .on("show.bs.dropdown", function () {
        showingAll = false;
        loadNotifications();
      });

    // Mark All Read button
    $("#markAllReadBtn").on("click", function (e) {
      e.preventDefault();
      e.stopPropagation();
      markAllAsRead();
    });

    // View All button - expands to show all notifications
    $("#viewAllNotificationsBtn").on("click", function (e) {
      e.preventDefault();
      e.stopPropagation();
      showingAll = true;
      renderNotifications(notifications);
      $(this).text("Showing all notifications").prop("disabled", true);
    });
  }

  // Load unread count for the badge
  function loadUnreadCount() {
    $.ajax({
      url: "/api/Notification/count",
      type: "GET",
      success: function (response) {
        updateBadge(response.count);
      },
      error: function () {
        console.error("Failed to load notification count");
      },
    });
  }

  // Load notification list
  function loadNotifications() {
    const listContainer = $("#notificationListContainer");
    listContainer.html(`
            <div class="notification-loading">
                <div class="spinner-border spinner-border-sm text-primary" role="status">
                    <span class="sr-only">Loading...</span>
                </div>
            </div>
        `);

    $.ajax({
      url: "/api/Notification/list",
      type: "GET",
      data: { take: 50 },
      success: function (data) {
        notifications = data;
        renderNotifications(data);
      },
      error: function () {
        listContainer.html(`
                    <div class="notification-empty">
                        <i class="fas fa-exclamation-triangle text-warning"></i>
                        <p class="mb-0 text-muted">Failed to load</p>
                    </div>
                `);
      },
    });
  }

  // Update the bell badge
  function updateBadge(count) {
    const badge = $("#notificationBadge");
    const bell = $(".notification-bell");

    if (count > 0) {
      badge.text(count > 99 ? "99+" : count);
      badge.show();
      bell.addClass("has-notifications");
    } else {
      badge.hide();
      bell.removeClass("has-notifications");
    }
  }

  // Get icon and color for notification type
  function getNotificationStyle(title) {
    const t = (title || "").toLowerCase();

    if (t.includes("booking") && (t.includes("new") || t.includes("request"))) {
      return {
        icon: "fas fa-calendar-plus",
        color: "primary",
        type: "booking",
      };
    }
    if (t.includes("booking")) {
      return { icon: "fas fa-calendar-check", color: "info", type: "booking" };
    }
    if (t.includes("payment") || t.includes("paid")) {
      return { icon: "fas fa-credit-card", color: "success", type: "payment" };
    }
    if (t.includes("approved") || t.includes("verified")) {
      return {
        icon: "fas fa-check-circle",
        color: "success",
        type: "approval",
      };
    }
    if (t.includes("rejected") || t.includes("declined")) {
      return {
        icon: "fas fa-times-circle",
        color: "danger",
        type: "rejection",
      };
    }
    if (t.includes("blocked")) {
      return { icon: "fas fa-ban", color: "danger", type: "blocked" };
    }
    if (t.includes("unblocked")) {
      return { icon: "fas fa-unlock", color: "success", type: "unblocked" };
    }
    if (t.includes("review")) {
      return { icon: "fas fa-star", color: "warning", type: "review" };
    }
    if (t.includes("verification")) {
      return { icon: "fas fa-user-check", color: "info", type: "verification" };
    }
    if (t.includes("category")) {
      return { icon: "fas fa-folder-plus", color: "info", type: "category" };
    }

    return { icon: "fas fa-bell", color: "secondary", type: "general" };
  }

  // Get action URL based on notification title - returns null for blocked/unblocked (user can't access)
  function getActionUrl(title) {
    const t = (title || "").toLowerCase();

    // Skip action URLs for blocked/unblocked - users don't have access to admin pages
    if (t.includes("blocked") || t.includes("unblocked")) {
      return null;
    }

    for (const [keyword, url] of Object.entries(ACTION_URL_MAP)) {
      if (t.includes(keyword)) {
        return url;
      }
    }
    return null;
  }

  // Render the list inside the dropdown
  function renderNotifications(items) {
    const listContainer = $("#notificationListContainer");
    const viewAllBtn = $("#viewAllNotificationsBtn");

    if (!items || items.length === 0) {
      listContainer.html(`
                <div class="notification-empty">
                    <i class="far fa-bell-slash fa-2x text-muted mb-2"></i>
                    <p class="mb-0 text-muted">No notifications</p>
                </div>
            `);
      viewAllBtn.hide();
      return;
    }

    // Determine how many to show
    const displayItems = showingAll ? items : items.slice(0, PREVIEW_COUNT);
    const hasMore = items.length > PREVIEW_COUNT;

    let html = "";
    displayItems.forEach(function (notification) {
      const notificationId = getNotificationId(notification);
      const unreadClass = !notification.isRead ? "unread" : "";
      const style = getNotificationStyle(notification.title);
      const actionUrl = getActionUrl(notification.title);

      html += `
                <div class="notification-item ${unreadClass}" data-id="${notificationId}">
                    <div class="notification-icon bg-${style.color}">
                        <i class="${style.icon}"></i>
                    </div>
                    <div class="notification-content">
                        <div class="notification-title">${escapeHtml(
                          notification.title
                        )}</div>
                        <div class="notification-message">${escapeHtml(
                          notification.message
                        )}</div>
                        <div class="notification-meta">
                            <span class="notification-time"><i class="far fa-clock mr-1"></i>${
                              notification.timeAgo
                            }</span>
                            ${
                              actionUrl
                                ? `<a href="${actionUrl}" class="notification-action" onclick="event.stopPropagation();">View <i class="fas fa-arrow-right ml-1"></i></a>`
                                : ""
                            }
                        </div>
                    </div>
                    <button type="button" class="notification-delete" data-id="${notificationId}" title="Delete">
                        <i class="fas fa-times"></i>
                    </button>
                </div>
            `;
    });

    listContainer.html(html);

    // Show/hide View All button
    if (hasMore && !showingAll) {
      viewAllBtn
        .text(`View All (${items.length})`)
        .prop("disabled", false)
        .show();
    } else {
      viewAllBtn.hide();
    }

    // Attach click handlers to items - show details
    $(".notification-item").on("click", function (e) {
      if (
        !$(e.target).closest(".notification-delete, .notification-action")
          .length
      ) {
        const id = $(this).data("id");
        showNotificationDetails(id);
      }
    });

    // Delete button handlers
    $(".notification-delete").on("click", function (e) {
      e.preventDefault();
      e.stopPropagation();
      const id = $(this).data("id");
      deleteNotification(id);
    });
  }

  // Show notification details in a SweetAlert modal
  function showNotificationDetails(id) {
    const notification = notifications.find((n) => getNotificationId(n) === id);
    if (!notification) return;

    // Mark as read
    if (!notification.isRead) {
      markAsRead(id);
      $(`.notification-item[data-id="${id}"]`).removeClass("unread");
    }

    const style = getNotificationStyle(notification.title);
    const actionUrl = getActionUrl(notification.title);

    // Build meta info if available
    let metaHtml = "";
    if (notification.metaJson) {
      try {
        const meta = JSON.parse(notification.metaJson);
        if (meta.reason) {
          metaHtml = `<div class="alert alert-warning mt-3 text-left"><strong>Reason:</strong><br>${escapeHtml(
            meta.reason
          )}</div>`;
        }
      } catch (e) {
        // Ignore JSON parse errors
      }
    }

    // SweetAlert for details
    if (typeof Swal !== "undefined") {
      Swal.fire({
        title: `<i class="${style.icon} text-${
          style.color
        } mr-2"></i> ${escapeHtml(notification.title)}`,
        html: `
                    <div class="text-left">
                        <p class="mb-2">${escapeHtml(notification.message)}</p>
                        <small class="text-muted"><i class="far fa-clock mr-1"></i>${
                          notification.timeAgo
                        }</small>
                        ${metaHtml}
                    </div>
                `,
        showCancelButton: !!actionUrl,
        confirmButtonText: actionUrl ? "Go to Page" : "Close",
        cancelButtonText: "Close",
        confirmButtonColor: "#3f72af",
        cancelButtonColor: "#6c757d",
        customClass: {
          popup: "notification-detail-popup",
        },
      }).then((result) => {
        if (result.isConfirmed && actionUrl) {
          window.location.href = actionUrl;
        }
      });
    }
  }

  function markAsRead(id) {
    $.ajax({
      url: `/api/Notification/${id}/mark-read`,
      type: "POST",
      success: function (response) {
        const n = notifications.find((x) => getNotificationId(x) === id);
        if (n) n.isRead = true;
        updateBadge(response.unreadCount);
      },
    });
  }

  function markAllAsRead() {
    $.ajax({
      url: "/api/Notification/mark-all-read",
      type: "POST",
      success: function (response) {
        updateBadge(0);
        $(".notification-item").removeClass("unread");

        if (typeof Swal !== "undefined") {
          Swal.fire({
            icon: "success",
            title: "All marked as read",
            toast: true,
            position: "top-end",
            showConfirmButton: false,
            timer: 2000,
          });
        }
      },
    });
  }

  function deleteNotification(id) {
    $.ajax({
      url: `/api/Notification/${id}`,
      type: "DELETE",
      success: function (response) {
        // Remove from local array
        notifications = notifications.filter(
          (n) => getNotificationId(n) !== id
        );

        // Remove from DOM
        $(`.notification-item[data-id="${id}"]`).fadeOut(200, function () {
          $(this).remove();

          // Check if list is now empty or needs update
          if (notifications.length === 0) {
            $("#notificationListContainer").html(`
                            <div class="notification-empty">
                                <i class="far fa-bell-slash fa-2x text-muted mb-2"></i>
                                <p class="mb-0 text-muted">No notifications</p>
                            </div>
                        `);
            $("#viewAllNotificationsBtn").hide();
          } else if (!showingAll && notifications.length > PREVIEW_COUNT) {
            // Re-render to show next item
            renderNotifications(notifications);
          }
        });

        // Update badge
        updateBadge(response.unreadCount);

        if (typeof Swal !== "undefined") {
          Swal.fire({
            icon: "success",
            title: "Notification deleted",
            toast: true,
            position: "top-end",
            showConfirmButton: false,
            timer: 1500,
          });
        }
      },
      error: function () {
        if (typeof Swal !== "undefined") {
          Swal.fire({
            icon: "error",
            title: "Failed to delete",
            toast: true,
            position: "top-end",
            showConfirmButton: false,
            timer: 2000,
          });
        }
      },
    });
  }

  function escapeHtml(text) {
    if (!text) return "";
    const map = {
      "&": "&amp;",
      "<": "&lt;",
      ">": "&gt;",
      '"': "&quot;",
      "'": "&#039;",
    };
    return text.replace(/[&<>"']/g, (m) => map[m]);
  }

  function getNotificationId(notification) {
    return notification.notificationId || notification.id || "";
  }

  // Expose global
  window.notificationPanel = {
    reload: function () {
      loadUnreadCount();
      if ($("#notificationDropdownToggle").parent().hasClass("show")) {
        loadNotifications();
      }
    },
  };
})();
