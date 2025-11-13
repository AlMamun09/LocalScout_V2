/**
 * Form Validation Module
 * Handles client-side form validation for registration forms
 */

export class FormValidation {
  constructor(options = {}) {
    this.formId = options.formId || "registerForm";
    this.currentStep = options.currentStep || 1;
    this.validationRules = options.validationRules || {};

    this.init();
  }

  init() {
    this.attachEventListeners();
  }

  attachEventListeners() {
    // Real-time validation
    document.querySelectorAll("input").forEach((input) => {
      input.addEventListener("blur", () => {
        this.validateOnBlur();
      });

      input.addEventListener("input", () => {
        if (input.classList.contains("is-invalid") && input.value.trim()) {
          input.classList.remove("is-invalid");
        }
      });
    });

    // Confirm password real-time validation
    const confirmPassword = document.getElementById("confirmPasswordInput");
    const password = document.getElementById("passwordInput");

    if (confirmPassword && password) {
      confirmPassword.addEventListener("input", () => {
        if (confirmPassword.value && confirmPassword.value === password.value) {
          confirmPassword.classList.remove("is-invalid");
          confirmPassword.classList.add("is-valid");
          this.hideError(confirmPassword);
        }
      });
    }

    // Prevent Enter key from submitting form unexpectedly
    const form = document.getElementById(this.formId);
    if (form) {
      form.addEventListener("keypress", (e) => {
        if (
          e.key === "Enter" &&
          e.target.tagName !== "TEXTAREA" &&
          e.target.type !== "submit"
        ) {
          e.preventDefault();
        }
      });
    }
  }

  validateOnBlur() {
    // Override this method or pass validation rules in subclasses
    return true;
  }

  validateAccountInfo() {
    let isValid = true;

    // Full Name validation
    const fullName = document.getElementById("Input_FullName");
    if (fullName) {
      if (!fullName.value.trim()) {
        isValid = false;
        this.markInvalid(fullName, "Full name is required");
      } else {
        this.markValid(fullName);
      }
    }

    // Email validation
    const email = document.getElementById("Input_Email");
    if (email) {
      const atChar = String.fromCharCode(64);
      const emailRegex = new RegExp(
        "^[^\\s" +
          atChar +
          "]+" +
          atChar +
          "[^\\s" +
          atChar +
          "]+\\.[^\\s" +
          atChar +
          "]+$"
      );

      if (!email.value.trim()) {
        isValid = false;
        this.markInvalid(email, "Email is required");
      } else if (!emailRegex.test(email.value)) {
        isValid = false;
        this.markInvalid(email, "Please enter a valid email address");
      } else {
        this.markValid(email);
      }
    }

    // Password validation
    const password = document.getElementById("passwordInput");
    if (password) {
      if (!password.value) {
        isValid = false;
        this.markInvalid(password, "Password is required");
      } else if (password.value.length < 6) {
        isValid = false;
        this.markInvalid(password, "Password must be at least 6 characters");
      } else {
        this.markValid(password);
      }
    }

    // Confirm Password validation
    const confirmPassword = document.getElementById("confirmPasswordInput");
    if (confirmPassword && password) {
      if (!confirmPassword.value) {
        isValid = false;
        this.markInvalid(confirmPassword, "Please confirm your password");
      } else if (confirmPassword.value !== password.value) {
        isValid = false;
        this.markInvalid(confirmPassword, "Passwords do not match");
      } else {
        this.markValid(confirmPassword);
      }
    }

    return isValid;
  }

  validatePersonalDetails() {
    let isValid = true;

    // Phone Number validation
    const phone = document.getElementById("Input_PhoneNumber");
    if (phone) {
      if (!phone.value.trim()) {
        isValid = false;
        this.markInvalid(phone, "Phone number is required");
      } else {
        this.markValid(phone);
      }
    }

    // Address validation
    const address = document.getElementById("addressInput");
    if (address) {
      if (!address.value.trim()) {
        isValid = false;
        this.markInvalid(address, "Address is required");
      } else {
        //this.markValid(address);
      }
    }

    return isValid;
  }

  markInvalid(input, message) {
    input.classList.add("is-invalid");
    input.classList.remove("is-valid");
    this.showError(input, message);
  }

  markValid(input) {
    input.classList.remove("is-invalid");
    input.classList.add("is-valid");
    this.hideError(input);
  }

  showError(input, message) {
    const errorSpan = input
      .closest(".form-group")
      ?.querySelector(".text-danger");
    if (errorSpan && !errorSpan.hasAttribute("data-valmsg-for")) {
      errorSpan.textContent = message;
    }
  }

  hideError(input) {
    const errorSpan = input
      .closest(".form-group")
      ?.querySelector(".text-danger");
    if (errorSpan && !errorSpan.hasAttribute("data-valmsg-for")) {
      errorSpan.textContent = "";
    }
  }
}
