/**
 * Password Strength Module
 * Evaluates and displays password strength based on backend requirements:
 * - Minimum 8 characters
 * - At least one lowercase letter
 * - At least one uppercase letter
 * - At least one digit
 */

export class PasswordStrength {
  constructor(options = {}) {
    this.passwordInputId = options.passwordInputId || "passwordInput";
    this.strengthBarId = options.strengthBarId || "passwordStrengthBar";
    this.strengthTextId = options.strengthTextId || "passwordStrengthText";
    this.strengthContainerId =
      options.strengthContainerId || "passwordStrength";

    this.init();
  }

  init() {
    const passwordInput = document.getElementById(this.passwordInputId);
    const strengthBar = document.getElementById(this.strengthBarId);
    const strengthText = document.getElementById(this.strengthTextId);
    const strengthContainer = document.getElementById(this.strengthContainerId);

    if (!passwordInput || !strengthBar || !strengthText || !strengthContainer) {
      console.warn("PasswordStrength: Required elements not found");
      return;
    }

    passwordInput.addEventListener("input", () => {
      const password = passwordInput.value;

      if (password.length === 0) {
        strengthContainer.classList.remove("show");
        strengthText.textContent = "";
        return;
      }

      strengthContainer.classList.add("show");

      const { strength, meetsRequirements, details } =
        this.calculateStrength(password);
      const { color, text } = this.getStrengthInfo(
        strength,
        meetsRequirements,
        details
      );

      strengthBar.style.width = strength + "%";
      strengthBar.style.backgroundColor = color;
      strengthText.style.color = color;
      strengthText.textContent = text;
    });
  }

  /**
   * Calculate password strength based on backend requirements
   * Returns strength percentage and whether all requirements are met
   */
  calculateStrength(password) {
    let strength = 0;
    const details = {
      length: false,
      lowercase: false,
      uppercase: false,
      digit: false,
      special: false,
    };

    // Required: minimum 8 characters (20%)
    if (password.length >= 8) {
      strength += 20;
      details.length = true;
    }

    // Required: at least one lowercase letter (20%)
    if (/[a-z]/.test(password)) {
      strength += 20;
      details.lowercase = true;
    }

    // Required: at least one uppercase letter (20%)
    if (/[A-Z]/.test(password)) {
      strength += 20;
      details.uppercase = true;
    }

    // Required: at least one digit (20%)
    if (/[0-9]/.test(password)) {
      strength += 20;
      details.digit = true;
    }

    // Bonus: special character (not required but strengthens) (20%)
    if (/[^A-Za-z0-9]/.test(password)) {
      strength += 20;
      details.special = true;
    }

    // Check if all required criteria are met
    const meetsRequirements =
      details.length && details.lowercase && details.uppercase && details.digit;

    return { strength, meetsRequirements, details };
  }

  /**
   * Get strength display information
   */
  getStrengthInfo(strength, meetsRequirements, details) {
    if (!meetsRequirements) {
      // Build helpful message about what's missing
      const missing = [];
      if (!details.length) missing.push("8+ chars");
      if (!details.lowercase) missing.push("lowercase");
      if (!details.uppercase) missing.push("uppercase");
      if (!details.digit) missing.push("number");

      return {
        color: "#dc3545",
        text: `Missing: ${missing.join(", ")}`,
      };
    } else if (strength === 80) {
      // Meets all required criteria
      return {
        color: "#17a2b8",
        text: "Good - Meets requirements",
      };
    } else {
      // Has special character bonus
      return {
        color: "#28a745",
        text: "Strong",
      };
    }
  }
}
