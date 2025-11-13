/**
 * Password Strength Module
 * Evaluates and displays password strength
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
        return;
      }

      strengthContainer.classList.add("show");

      const strength = this.calculateStrength(password);
      const { color, text } = this.getStrengthInfo(strength);

      strengthBar.style.width = strength + "%";
      strengthBar.style.backgroundColor = color;
      strengthText.style.color = color;
      strengthText.textContent = text;
    });
  }

  calculateStrength(password) {
    let strength = 0;

    if (password.length >= 8) strength += 25;
    if (/[a-z]/.test(password)) strength += 25;
    if (/[A-Z]/.test(password)) strength += 25;
    if (/[0-9]/.test(password) || /[^A-Za-z0-9]/.test(password)) strength += 25;

    return strength;
  }

  getStrengthInfo(strength) {
    if (strength <= 25) {
      return { color: "#dc3545", text: "Weak" };
    } else if (strength <= 50) {
      return { color: "#ffc107", text: "Fair" };
    } else if (strength <= 75) {
      return { color: "#17a2b8", text: "Good" };
    } else {
      return { color: "#28a745", text: "Strong" };
    }
  }
}
