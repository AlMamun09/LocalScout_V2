/**
 * Form Validation Module
 * Handles client-side form validation for registration forms
 * Enforces complete validation before step progression
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
    // Real-time validation on blur
    document.querySelectorAll("input, textarea, select").forEach((input) => {
      input.addEventListener("blur", () => {
        this.validateOnBlur(input);
      });

      input.addEventListener("input", () => {
        // Remove invalid state when user starts typing
        if (input.classList.contains("is-invalid") && input.value.trim()) {
          input.classList.remove("is-invalid");
          this.hideError(input);
        }
      });
    });

    // Confirm password real-time validation
    const confirmPassword = document.getElementById("confirmPasswordInput");
    const password = document.getElementById("passwordInput");

    if (confirmPassword && password) {
      confirmPassword.addEventListener("input", () => {
        // Only show valid if passwords match AND password meets requirements
        const passwordResult = this.validatePassword(password.value);
        if (
          confirmPassword.value &&
          confirmPassword.value === password.value &&
          passwordResult.valid
        ) {
          confirmPassword.classList.remove("is-invalid");
          confirmPassword.classList.add("is-valid");
          this.hideError(confirmPassword);
        } else if (
          confirmPassword.value &&
          confirmPassword.value === password.value &&
          !passwordResult.valid
        ) {
          // Passwords match but password is weak - don't show as valid
          confirmPassword.classList.remove("is-valid");
          confirmPassword.classList.remove("is-invalid");
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

  validateOnBlur(input) {
    // Skip validation for hidden inputs
    if (input.type === "hidden") return;

    const fieldName = input.name || input.id;

    // Only validate if field has a value or is required
    if (!input.value.trim() && !input.hasAttribute("required")) {
      return;
    }

    // Trigger appropriate validation based on field
    if (fieldName.includes("Password") && !fieldName.includes("Confirm")) {
      this.validatePasswordField(input);
    } else if (fieldName.includes("Email")) {
      this.validateEmailField(input);
    } else if (fieldName.includes("PhoneNumber")) {
      this.validatePhoneField(input);
    }
  }

  validatePasswordField(input) {
    const result = this.validatePassword(input.value);
    if (!result.valid) {
      this.markInvalid(input, result.errors[0]);
    } else {
      this.markValid(input);
    }
  }

  validateEmailField(input) {
    const result = this.validateEmail(input.value);
    if (!result.valid) {
      this.markInvalid(input, result.error);
    } else {
      this.markValid(input);
    }
  }

  validatePhoneField(input) {
    const result = this.validatePhoneNumber(input.value);
    if (!result.valid) {
      this.markInvalid(input, result.error);
    } else {
      this.markValid(input);
    }
  }

  /**
   * Step 1: Account Information Validation
   * - Full Name: Required, 2-100 characters
   * - Email: Required, valid format
   * - Password: Required, 8-100 chars, uppercase, lowercase, digit
   * - Confirm Password: Required, must match password
   */
  validateAccountInfo() {
    let isValid = true;

    // Full Name validation
    const fullName = document.getElementById("Input_FullName");
    if (fullName) {
      const value = fullName.value.trim();
      if (!value) {
        isValid = false;
        this.markInvalid(fullName, "Full name is required");
      } else if (value.length < 2) {
        isValid = false;
        this.markInvalid(fullName, "Full name must be at least 2 characters");
      } else if (value.length > 100) {
        isValid = false;
        this.markInvalid(
          fullName,
          "Full name must be less than 100 characters"
        );
      } else {
        this.markValid(fullName);
      }
    }

    // Email validation
    const email = document.getElementById("Input_Email");
    if (email) {
      const result = this.validateEmail(email.value);
      if (!result.valid) {
        isValid = false;
        this.markInvalid(email, result.error);
      } else {
        this.markValid(email);
      }
    }

    // Password validation with complexity rules
    const password = document.getElementById("passwordInput");
    if (password) {
      const result = this.validatePassword(password.value);
      if (!result.valid) {
        isValid = false;
        this.markInvalid(password, result.errors[0]);
      } else {
        this.markValid(password);
      }
    }

    // Confirm Password validation
    const confirmPassword = document.getElementById("confirmPasswordInput");
    if (confirmPassword && password) {
      const passwordResult = this.validatePassword(password.value);
      if (!confirmPassword.value) {
        isValid = false;
        this.markInvalid(confirmPassword, "Please confirm your password");
      } else if (confirmPassword.value !== password.value) {
        isValid = false;
        this.markInvalid(confirmPassword, "Passwords do not match");
      } else if (!passwordResult.valid) {
        // Passwords match but password is weak - don't mark as valid
        confirmPassword.classList.remove("is-valid");
        confirmPassword.classList.remove("is-invalid");
        this.hideError(confirmPassword);
      } else {
        this.markValid(confirmPassword);
      }
    }

    return isValid;
  }

  /**
   * Step 2: Personal Details Validation
   * - Date of Birth: Required, age 18+
   * - Gender: Required
   * - Phone Number: Required, valid format
   * - Address: Required
   */
  validatePersonalDetails() {
    let isValid = true;

    // Date of Birth validation with age check
    const dob = document.getElementById("Input_DateOfBirth");
    if (dob) {
      const result = this.validateDateOfBirth(dob.value);
      if (!result.valid) {
        isValid = false;
        this.markInvalid(dob, result.error);
      } else {
        this.markValid(dob);
      }
    }

    // Gender validation - check if any radio is selected
    const genderInputs = document.querySelectorAll(
      'input[name="Input.Gender"]'
    );
    if (genderInputs.length > 0) {
      const genderSelected = Array.from(genderInputs).some(
        (input) => input.checked
      );
      const genderContainer = genderInputs[0].closest(".form-group");

      if (!genderSelected) {
        isValid = false;
        // Mark all gender labels as invalid
        genderInputs.forEach((input) => {
          const label = document.querySelector(`label[for="${input.id}"]`);
          if (label) {
            label.classList.add("border-red-500");
          }
        });
        // Show error message
        const errorSpan = genderContainer?.querySelector(".text-danger");
        if (errorSpan) {
          errorSpan.textContent = "Please select a gender";
        }
      } else {
        // Clear invalid states
        genderInputs.forEach((input) => {
          const label = document.querySelector(`label[for="${input.id}"]`);
          if (label) {
            label.classList.remove("border-red-500");
          }
        });
        const errorSpan = genderContainer?.querySelector(".text-danger");
        if (errorSpan) {
          errorSpan.textContent = "";
        }
      }
    }

    // Phone Number validation with format check
    const phone = document.getElementById("Input_PhoneNumber");
    if (phone) {
      const result = this.validatePhoneNumber(phone.value);
      if (!result.valid) {
        isValid = false;
        this.markInvalid(phone, result.error);
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
        this.markValid(address);
      }
    }

    return isValid;
  }

  /**
   * Step 3: Business Information Validation (Provider Only)
   * - Working Days: Required
   * - Working Hours: Required
   * - Description: Required, 10+ characters
   */
  validateBusinessInfo() {
    let isValid = true;

    // Working Days validation
    const workingDaysInput = document.getElementById("workingDaysInput");
    const workingDayStart = document.getElementById("workingDayStart");
    const workingDayEnd = document.getElementById("workingDayEnd");

    if (workingDaysInput) {
      if (!workingDaysInput.value.trim()) {
        isValid = false;
        // Mark the dropdowns as invalid
        if (workingDayStart) {
          workingDayStart.classList.add("is-invalid");
        }
        if (workingDayEnd) {
          workingDayEnd.classList.add("is-invalid");
        }
        // Show error
        const container = workingDaysInput.closest(".form-group");
        const errorSpan = container?.querySelector(".text-danger");
        if (errorSpan) {
          errorSpan.textContent =
            "Working days are required. Please select start and end days.";
        }
      } else {
        if (workingDayStart) {
          workingDayStart.classList.remove("is-invalid");
          workingDayStart.classList.add("is-valid");
        }
        if (workingDayEnd) {
          workingDayEnd.classList.remove("is-invalid");
          workingDayEnd.classList.add("is-valid");
        }
        const container = workingDaysInput.closest(".form-group");
        const errorSpan = container?.querySelector(".text-danger");
        if (errorSpan) {
          errorSpan.textContent = "";
        }
      }
    }

    // Working Hours validation
    const workingHoursInput = document.getElementById("workingHoursInput");
    const workingHoursStart = document.getElementById("workingHoursStart");
    const workingHoursEnd = document.getElementById("workingHoursEnd");

    if (workingHoursInput) {
      if (!workingHoursInput.value.trim()) {
        isValid = false;
        // Mark the time inputs as invalid
        if (workingHoursStart) {
          workingHoursStart.classList.add("is-invalid");
        }
        if (workingHoursEnd) {
          workingHoursEnd.classList.add("is-invalid");
        }
        // Show error
        const container = workingHoursInput.closest(".form-group");
        const errorSpan = container?.querySelector(".text-danger");
        if (errorSpan) {
          errorSpan.textContent =
            "Working hours are required. Please select start and end times.";
        }
      } else {
        if (workingHoursStart) {
          workingHoursStart.classList.remove("is-invalid");
          workingHoursStart.classList.add("is-valid");
        }
        if (workingHoursEnd) {
          workingHoursEnd.classList.remove("is-invalid");
          workingHoursEnd.classList.add("is-valid");
        }
        const container = workingHoursInput.closest(".form-group");
        const errorSpan = container?.querySelector(".text-danger");
        if (errorSpan) {
          errorSpan.textContent = "";
        }
      }
    }

    // Description validation
    const description = document.getElementById("providerDescriptionTextarea");
    if (description) {
      const value = description.value.trim();
      if (!value) {
        isValid = false;
        this.markInvalid(description, "Business description is required");
      } else if (value.length < 10) {
        isValid = false;
        this.markInvalid(
          description,
          "Description must be at least 10 characters"
        );
      } else if (value.length > 2000) {
        isValid = false;
        this.markInvalid(
          description,
          "Description must be less than 2000 characters"
        );
      } else {
        this.markValid(description);
      }
    }

    return isValid;
  }

  /**
   * Validate email format
   */
  validateEmail(email) {
    if (!email || !email.trim()) {
      return { valid: false, error: "Email is required" };
    }

    // Standard email regex
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

    if (!emailRegex.test(email)) {
      return { valid: false, error: "Please enter a valid email address" };
    }

    return { valid: true };
  }

  /**
   * Validate password with complexity requirements
   * - Minimum 8 characters
   * - Maximum 100 characters
   * - At least one uppercase letter
   * - At least one lowercase letter
   * - At least one digit
   */
  validatePassword(password) {
    const errors = [];

    if (!password) {
      return { valid: false, errors: ["Password is required"] };
    }

    if (password.length < 8) {
      errors.push("Password must be at least 8 characters");
    }

    if (password.length > 100) {
      errors.push("Password must be less than 100 characters");
    }

    if (!/[a-z]/.test(password)) {
      errors.push("Password must contain at least one lowercase letter");
    }

    if (!/[A-Z]/.test(password)) {
      errors.push("Password must contain at least one uppercase letter");
    }

    if (!/[0-9]/.test(password)) {
      errors.push("Password must contain at least one number");
    }

    return { valid: errors.length === 0, errors };
  }

  /**
   * Validate date of birth (must be 18+)
   */
  validateDateOfBirth(dobValue) {
    if (!dobValue) {
      return { valid: false, error: "Date of birth is required" };
    }

    const birthDate = new Date(dobValue);
    const today = new Date();

    // Check if date is valid
    if (isNaN(birthDate.getTime())) {
      return { valid: false, error: "Please enter a valid date" };
    }

    // Calculate age
    let age = today.getFullYear() - birthDate.getFullYear();
    const monthDiff = today.getMonth() - birthDate.getMonth();

    if (
      monthDiff < 0 ||
      (monthDiff === 0 && today.getDate() < birthDate.getDate())
    ) {
      age--;
    }

    if (age < 18) {
      return { valid: false, error: "You must be at least 18 years old" };
    }

    // Check if date is not in the future
    if (birthDate > today) {
      return { valid: false, error: "Date of birth cannot be in the future" };
    }

    return { valid: true };
  }

  /**
   * Validate phone number format
   */
  validatePhoneNumber(phone) {
    if (!phone || !phone.trim()) {
      return { valid: false, error: "Phone number is required" };
    }

    // Remove common formatting characters for validation
    const cleanPhone = phone.replace(/[\s\-\(\)\.]/g, "");

    // Regex for common phone formats (supports international)
    // Allows optional + at start, then 7-15 digits
    const phoneRegex = /^\+?[0-9]{7,15}$/;

    if (!phoneRegex.test(cleanPhone)) {
      return { valid: false, error: "Please enter a valid phone number" };
    }

    return { valid: true };
  }

  /**
   * Mark an input as invalid and show error message
   */
  markInvalid(input, message) {
    input.classList.add("is-invalid");
    input.classList.remove("is-valid");
    this.showError(input, message);
  }

  /**
   * Mark an input as valid and hide error message
   */
  markValid(input) {
    input.classList.remove("is-invalid");
    input.classList.add("is-valid");
    this.hideError(input);
  }

  /**
   * Show error message near input field
   */
  showError(input, message) {
    const formGroup = input.closest(".form-group");
    if (!formGroup) return;

    const errorSpan = formGroup.querySelector(".text-danger");
    if (errorSpan) {
      errorSpan.textContent = message;
    }
  }

  /**
   * Hide error message
   */
  hideError(input) {
    const formGroup = input.closest(".form-group");
    if (!formGroup) return;

    const errorSpan = formGroup.querySelector(".text-danger");
    if (errorSpan && !errorSpan.hasAttribute("data-valmsg-for")) {
      errorSpan.textContent = "";
    }
  }
}
