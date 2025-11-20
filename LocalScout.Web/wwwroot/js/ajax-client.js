const AjaxClient = {
  // Generic POST with Anti-Forgery Header
  post: function (url, data, successCallback, errorCallback) {
    $.ajax({
      url: url,
      type: "POST",
      data: data,
      headers: {
        RequestVerificationToken: $(
          'input[name="__RequestVerificationToken"]'
        ).val(),
      },
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
