/**
 * Multi-Step Form Module
 * Handles step navigation and progress tracking
 */

export class MultiStepForm {
  constructor(options = {}) {
    this.currentStep = 1;
    this.totalSteps = options.totalSteps || 2;
    this.formId = options.formId || "registerForm";
    this.progressLineId = options.progressLineId || "progressLine";
    this.onStepChange = options.onStepChange || null;
    this.onValidateStep = options.onValidateStep || null;

    // Expose methods globally for inline onclick handlers (if required)
    window.nextStep = () => this.nextStep();
    window.prevStep = () => this.prevStep();

    this.init();
  }

  init() {
    this.updateProgress();
    this.attachFormSubmitHandler();
  }

  attachFormSubmitHandler() {
    const form = document.getElementById(this.formId);
    if (!form) return;

    form.addEventListener("submit", (e) => {
      if (this.onValidateStep && !this.onValidateStep(this.currentStep)) {
        e.preventDefault();
        return false;
      }
      return true;
    });
  }

  nextStep() {
    // Validate current step before proceeding
    if (this.onValidateStep && !this.onValidateStep(this.currentStep)) {
      return;
    }

    if (this.currentStep < this.totalSteps) {
      const currentStepEl = document.querySelector(
        `.step[data-step="${this.currentStep}"]`
      );
      const currentFormStep = document.querySelector(
        `.form-step[data-step="${this.currentStep}"]`
      );

      if (currentStepEl && currentFormStep) {
        currentStepEl.classList.add("completed");
        currentStepEl.classList.remove("active");
        currentFormStep.classList.remove("active");
      }

      this.currentStep++;

      const nextStepEl = document.querySelector(
        `.step[data-step="${this.currentStep}"]`
      );
      const nextFormStep = document.querySelector(
        `.form-step[data-step="${this.currentStep}"]`
      );

      if (nextStepEl && nextFormStep) {
        nextStepEl.classList.add("active");
        nextFormStep.classList.add("active");
      }

      this.updateProgress();
      window.scrollTo({ top: 0, behavior: "smooth" });

      if (this.onStepChange) {
        this.onStepChange(this.currentStep);
      }
    }
  }

  prevStep() {
    if (this.currentStep > 1) {
      const currentStepEl = document.querySelector(
        `.step[data-step="${this.currentStep}"]`
      );
      const currentFormStep = document.querySelector(
        `.form-step[data-step="${this.currentStep}"]`
      );

      if (currentStepEl && currentFormStep) {
        currentStepEl.classList.remove("active");
        currentFormStep.classList.remove("active");
      }

      this.currentStep--;

      const prevStepEl = document.querySelector(
        `.step[data-step="${this.currentStep}"]`
      );
      const prevFormStep = document.querySelector(
        `.form-step[data-step="${this.currentStep}"]`
      );

      if (prevStepEl && prevFormStep) {
        prevStepEl.classList.add("active");
        prevStepEl.classList.remove("completed");
        prevFormStep.classList.add("active");
      }

      this.updateProgress();
      window.scrollTo({ top: 0, behavior: "smooth" });

      if (this.onStepChange) {
        this.onStepChange(this.currentStep);
      }
    }
  }

  updateProgress() {
    // Protect against division by zero when totalSteps is 1
    const total = this.totalSteps > 1 ? this.totalSteps : 1;
    const progress = ((this.currentStep - 1) / (total - 1)) * 100;
    const progressLine = document.getElementById(this.progressLineId);
    if (progressLine) {
      progressLine.style.width = progress + "%";
    }
  }

  getCurrentStep() {
    return this.currentStep;
  }
}
