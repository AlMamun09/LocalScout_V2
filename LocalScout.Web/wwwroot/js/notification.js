"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/notificationHub").build();

// Helper to update the notification UI
function addNotificationToUI(message, iconClass = "fas fa-envelope") {
    // 1. Update Badge Count
    var badge = $("#notificationBadge");
    var header = $("#notificationHeader");
    
    var currentCount = parseInt(badge.text()) || 0;
    var newCount = currentCount + 1;
    
    badge.text(newCount);
    header.text(newCount + " Notifications");

    // 2. Add to List
    var list = $("#notificationList");
    var noNotifMsg = list.find("p.text-muted");
    if (noNotifMsg.length > 0) {
        noNotifMsg.remove();
    }

    var time = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    
    var newItem = `
        <a href="#" class="dropdown-item">
            <i class="${iconClass} mr-2"></i> ${message}
            <span class="float-right text-muted text-sm">${time}</span>
        </a>
        <div class="dropdown-divider"></div>
    `;
    
    list.prepend(newItem);
}

// --- ADMIN: Receive New Request Notification ---
connection.on("ReceiveRequestNotification", function (message) {
    // Show Toast
    if (typeof Swal !== 'undefined') {
        Swal.fire({
            icon: 'info',
            title: 'New Request',
            text: message,
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: 5000
        });
    }

    // Update Bell Icon
    addNotificationToUI(message, "fas fa-user-plus");
});

// --- PROVIDER: Receive Status Update ---
connection.on("ReceiveStatusUpdate", function (status, message) {
    var iconType = status === 'Approved' ? 'success' : 'error';
    var iconClass = status === 'Approved' ? 'fas fa-check-circle text-success' : 'fas fa-times-circle text-danger';

    // Show Toast
    if (typeof Swal !== 'undefined') {
        Swal.fire({
            icon: iconType,
            title: 'Verification Update',
            text: message,
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: 5000
        });
    }

    // Update Bell Icon
    addNotificationToUI(message, iconClass);
});

connection.start().then(function () {
    console.log("SignalR Connected.");
}).catch(function (err) {
    return console.error(err.toString());
});
