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

// --- USER/PROVIDER: Receive Status Update ---
connection.on("ReceiveStatusUpdate", function (status, message) {
    var iconType = 'info';
    var iconClass = 'fas fa-info-circle text-info';
    var title = 'Status Update';
    var showFullAlert = false;

    // Determine alert type based on status
    switch(status) {
        case 'Approved':
            iconType = 'success';
            iconClass = 'fas fa-check-circle text-success';
            title = 'Verification Approved';
            break;
        case 'Rejected':
            iconType = 'warning';
            iconClass = 'fas fa-times-circle text-warning';
            title = 'Verification Rejected';
            break;
        case 'Blocked':
            iconType = 'error';
            iconClass = 'fas fa-ban text-danger';
            title = 'Account Blocked';
            showFullAlert = true; // Show full alert for blocked status
            break;
        case 'Unblocked':
            iconType = 'success';
            iconClass = 'fas fa-check-circle text-success';
            title = 'Account Unblocked';
            showFullAlert = true; // Show full alert for unblocked status
            break;
    }

    // For critical status changes (Block/Unblock), show full alert
    if (showFullAlert) {
        Swal.fire({
            icon: iconType,
            title: title,
            text: message,
            confirmButtonText: 'OK',
            confirmButtonColor: status === 'Blocked' ? '#d33' : '#28a745',
            allowOutsideClick: false
        }).then(() => {
            // If blocked, optionally redirect or show persistent warning
            if (status === 'Blocked') {
                showBlockedAccountBanner();
            }
            
            // Reload notifications after user acknowledges
            if (typeof window.notificationPanel !== 'undefined') {
                window.notificationPanel.reload();
            }
        });
    } else {
        // For non-critical updates, show toast
        Swal.fire({
            icon: iconType,
            title: title,
            text: message,
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: 5000,
            timerProgressBar: true
        });
        
        // Reload notifications immediately
        if (typeof window.notificationPanel !== 'undefined') {
            window.notificationPanel.reload();
        }
    }

    // Update Bell Icon (legacy support - will be replaced by notificationPanel.reload())
    addNotificationToUI(message, iconClass);

    // Play notification sound
    playNotificationSound();
});

// Show persistent blocked account banner
function showBlockedAccountBanner() {
    const banner = `
        <div class="alert alert-danger alert-dismissible fade show position-fixed" 
             style="top: 60px; right: 20px; z-index: 9999; max-width: 400px;" 
             role="alert">
            <strong><i class="fas fa-ban mr-2"></i>Account Blocked</strong>
            <p class="mb-0">Your account has been blocked. Please contact support for assistance.</p>
            <button type="button" class="close" data-dismiss="alert" aria-label="Close">
                <span aria-hidden="true">&times;</span>
            </button>
        </div>
    `;

    // Only add if not already present
    if ($('.alert-danger.position-fixed').length === 0) {
        $('body').append(banner);
    }
}

// Play notification sound (optional)
function playNotificationSound() {
    try {
        const audio = new Audio('/sounds/notification.mp3');
        audio.volume = 0.3;
        audio.play().catch(err => {
            console.log('Could not play notification sound:', err);
        });
    } catch (err) {
        console.log('Notification sound not available');
    }
}

// Start SignalR connection
connection.start().then(function () {
    console.log("SignalR Connected successfully.");
}).catch(function (err) {
    console.error("SignalR Connection Error:", err.toString());
    // Retry connection after 5 seconds
    setTimeout(() => {
        connection.start().catch(err => console.error("Reconnection failed:", err));
    }, 5000);
});

// Handle connection closed
connection.onclose(function () {
    console.log("SignalR connection closed. Attempting to reconnect...");
    setTimeout(() => {
        connection.start().catch(err => console.error("Reconnection failed:", err));
    }, 5000);
});

// Handle reconnecting
connection.onreconnecting(function (error) {
    console.log("SignalR reconnecting...", error);
});

// Handle reconnected
connection.onreconnected(function (connectionId) {
    console.log("SignalR reconnected. Connection ID:", connectionId);
});
