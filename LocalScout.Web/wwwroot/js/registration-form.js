/**
 * Registration Form Handler
 * Orchestrates all registration form functionality
 */

import { LocationService } from "./modules/location-service.js";
import { PasswordStrength } from "./modules/password-strength.js";
import { FormValidation } from "./modules/form-validation.js";
import { MultiStepForm } from "./modules/multi-step-form.js";

export class RegistrationForm {
  constructor(totalSteps = 2) {
    this.totalSteps = totalSteps;
    this.init();
  }

  init() {
    document.addEventListener("DOMContentLoaded", () => {
      // Initialize validation
      this.validator = new FormValidation({
        formId: "registerForm",
        currentStep: 1,
      });

      // Initialize multi-step form
      this.multiStepForm = new MultiStepForm({
        totalSteps: this.totalSteps,
        formId: "registerForm",
        progressLineId: "progressLine",
        onValidateStep: (step) => this.validateStep(step),
      });

      // Initialize password strength indicator
      this.passwordStrength = new PasswordStrength({
        passwordInputId: "passwordInput",
        strengthBarId: "passwordStrengthBar",
        strengthTextId: "passwordStrengthText",
        strengthContainerId: "passwordStrength",
      });

      // Initialize location service
      this.locationService = new LocationService({
        addressInputId: "addressInput",
        useLocationBtnId: "useLocationBtn",
        suggestionsId: "addressSuggestions",
        latitudeInputId: "latitudeInput",
        longitudeInputId: "longitudeInput",
      });

      // Make location service available globally for onclick handlers
      window.locationService = this.locationService;
    });
  }

  validateStep(step) {
    if (step === 1) {
      return this.validator.validateAccountInfo();
    } else if (step === 2) {
      return this.validator.validatePersonalDetails();
    }

    return true;
  }
}

// Auto-initialize if data attribute is present
if (document.querySelector("[data-registration-form]")) {
  const totalSteps =
    parseInt(
      document.querySelector("[data-registration-form]").dataset.totalSteps,
      10
    ) || 2;

  new RegistrationForm(totalSteps);
}
