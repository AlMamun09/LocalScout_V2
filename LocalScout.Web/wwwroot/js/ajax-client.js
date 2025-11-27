const AjaxClient = {
  // Generic POST with Anti-Forgery Token
  post: function (url, data, successCallback, errorCallback) {
    // Get the anti-forgery token from the hidden input field
    const token = $('input[name="__RequestVerificationToken"]').val();

    // Add the token to the data being sent
    const dataWithToken = $.extend({}, data, {
      __RequestVerificationToken: token,
    });

    $.ajax({
      url: url,
      type: "POST",
      data: dataWithToken,
      success: function (response) {
        if (successCallback) successCallback(response);
      },
      error: function (xhr, status, error) {
        if (errorCallback) {
          errorCallback(xhr);
        } else {
          console.error("AJAX Error:", xhr.responseText);
          // Fallback alert if SweetAlert isn't available
          alert("An error occurred: " + (xhr.responseJSON?.message || error));
        }
      },
    });
  },

  // Generic GET
  get: function (url, successCallback, errorCallback) {
    $.ajax({
      url: url,
      type: "GET",
      success: function (response) {
        if (successCallback) successCallback(response);
      },
      error: function (xhr, status, error) {
        if (errorCallback) errorCallback(xhr);
      },
    });
  },
};
