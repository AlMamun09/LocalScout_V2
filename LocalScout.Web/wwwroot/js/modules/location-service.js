/**
 * Location Service Module
 * Handles geolocation, address autocomplete, and reverse geocoding
 */

export class LocationService {
  constructor(options = {}) {
    this.addressInputId = options.addressInputId || "addressInput";
    this.useLocationBtnId = options.useLocationBtnId || "useLocationBtn";
    this.suggestionsId = options.suggestionsId || "addressSuggestions";
    this.latitudeInputId = options.latitudeInputId || "latitudeInput";
    this.longitudeInputId = options.longitudeInputId || "longitudeInput";
    this.debounceTimeout = null;
    this.debounceDelay = options.debounceDelay || 500;

    // Expose instance for external handlers (RegistrationForm sets window.locationService too)
    window.LocationServiceInstance = this;

    this.init();
  }

  init() {
    const addressInput = document.getElementById(this.addressInputId);
    const useLocationBtn = document.getElementById(this.useLocationBtnId);
    const suggestionsDiv = document.getElementById(this.suggestionsId);

    if (!addressInput || !useLocationBtn || !suggestionsDiv) {
      console.warn("LocationService: Required elements not found");
      return;
    }

    // Use My Location button
    useLocationBtn.addEventListener("click", () =>
      this.requestPreciseLocation()
    );

    // Address input for autocomplete (debounced)
    addressInput.addEventListener("input", (e) =>
      this.handleAddressInput(e.target.value)
    );

    // Click outside to hide suggestions
    document.addEventListener("click", (e) => {
      if (
        !addressInput.contains(e.target) &&
        !suggestionsDiv.contains(e.target)
      ) {
        suggestionsDiv.style.display = "none";
      }
    });
  }

  handleAddressInput(value) {
    clearTimeout(this.debounceTimeout);
    const query = value?.trim() || "";
    const suggestionsDiv = document.getElementById(this.suggestionsId);

    if (!suggestionsDiv) return;

    if (query.length < 3) {
      suggestionsDiv.style.display = "none";
      return;
    }

    this.debounceTimeout = setTimeout(() => {
      this.searchAddress(query);
    }, this.debounceDelay);
  }

  async requestPreciseLocation() {
    const useLocationBtn = document.getElementById(this.useLocationBtnId);
    const addressInput = document.getElementById(this.addressInputId);

    if (!useLocationBtn || !addressInput) return;

    if (!navigator.geolocation) {
      alert("Geolocation is not supported by your browser");
      return;
    }

    const setLoadingState = (isLoading, message) => {
      if (!useLocationBtn) return;
      if (isLoading) {
        useLocationBtn.innerHTML = `<i class="fas fa-spinner fa-spin"></i> ${message || 'Getting Location...'}`;
        useLocationBtn.disabled = true;
      } else {
        useLocationBtn.innerHTML = "Use My Location";
        useLocationBtn.disabled = false;
      }
    };

    setLoadingState(true, "Getting Location...");

    // Try multiple strategies in order of preference
    const strategies = [
      { enableHighAccuracy: true, timeout: 15000, maximumAge: 0, name: "High Accuracy" },
      { enableHighAccuracy: false, timeout: 30000, maximumAge: 30000, name: "Standard" },
      { enableHighAccuracy: false, timeout: 60000, maximumAge: 300000, name: "Cached/Network" },
    ];

    let position = null;
    let lastError = null;

    for (const strategy of strategies) {
      try {
        console.log(`Trying geolocation strategy: ${strategy.name}`);
        setLoadingState(true, `Trying ${strategy.name}...`);
        position = await this.getCurrentPositionAsync({
          enableHighAccuracy: strategy.enableHighAccuracy,
          timeout: strategy.timeout,
          maximumAge: strategy.maximumAge,
        });
        console.log(`Success with strategy: ${strategy.name}`);
        break;
      } catch (error) {
        console.warn(`Strategy ${strategy.name} failed:`, error.message || error.code);
        lastError = error;
        
        // If permission denied, don't try other strategies
        if (error.code === 1) { // PERMISSION_DENIED
          break;
        }
      }
    }

    if (!position) {
      this.handleGeolocationError(lastError, addressInput);
      setLoadingState(false);
      return;
    }

    const latitude = position.coords.latitude;
    const longitude = position.coords.longitude;
    const accuracy = position.coords.accuracy;

    console.log(
      `Location obtained: Lat ${latitude}, Lon ${longitude}, Accuracy: ${accuracy}m`
    );

    setLoadingState(true, "Getting Address...");

    try {
      const response = await fetch("/api/location/reverse-geocode", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ latitude, longitude }),
      });

      if (!response.ok) throw new Error("Failed to get address");

      const data = await response.json();

      // Autofill address and coordinates
      addressInput.value = data.displayName || "";
      const latEl = document.getElementById(this.latitudeInputId);
      const lonEl = document.getElementById(this.longitudeInputId);
      if (latEl) latEl.value = latitude;
      if (lonEl) lonEl.value = longitude;

      addressInput.classList.remove("is-invalid");
    } catch (error) {
      console.error("Reverse geocoding error:", error);
      // Still fill coordinates even if address lookup fails
      const latEl = document.getElementById(this.latitudeInputId);
      const lonEl = document.getElementById(this.longitudeInputId);
      if (latEl) latEl.value = latitude;
      if (lonEl) lonEl.value = longitude;
      
      alert("Location found but could not retrieve address. Please enter your address manually.");
      addressInput.focus();
    } finally {
      setLoadingState(false);
    }
  }

  getCurrentPositionAsync(options) {
    return new Promise((resolve, reject) => {
      navigator.geolocation.getCurrentPosition(resolve, reject, options);
    });
  }

  handleGeolocationError(error, addressInputEl) {
    console.error("Geolocation error:", error);

    let errorMessage = "Unable to get your location. ";

    if (!error) {
      errorMessage += "An unknown error occurred. Please enter your address manually.";
    } else {
      switch (error.code) {
        case 1: // PERMISSION_DENIED
          errorMessage +=
            "Location access was denied. Please allow location access in your browser settings or enter your address manually.";
          break;
        case 2: // POSITION_UNAVAILABLE
          errorMessage += "Location information is unavailable. Please ensure GPS is enabled or enter your address manually.";
          break;
        case 3: // TIMEOUT
          errorMessage += "Location request timed out. This may happen on slow networks or if GPS signal is weak. Please try again or enter your address manually.";
          break;
        default:
          errorMessage += "An unknown error occurred. Please enter your address manually.";
      }
    }

    alert(errorMessage);
    if (addressInputEl) addressInputEl.focus();
  }

  async searchAddress(query) {
    const suggestionsDiv = document.getElementById(this.suggestionsId);
    if (!suggestionsDiv) return;

    try {
      const response = await fetch(
        `/api/location/search?query=${encodeURIComponent(query)}`
      );
      if (!response.ok) throw new Error("Search failed");

      const suggestions = await response.json();
      if (!Array.isArray(suggestions) || suggestions.length === 0) {
        suggestionsDiv.style.display = "none";
        suggestionsDiv.innerHTML = "";
        return;
      }

      // Build suggestions list (avoid inline onclick + escaping)
      suggestionsDiv.innerHTML = "";
      suggestions.forEach((s) => {
        const btn = document.createElement("button");
        btn.type = "button";
        btn.className =
          "list-group-item list-group-item-action d-flex align-items-start";
        btn.innerHTML = `<i class="fas fa-map-marker-alt me-2"></i><span>${s.displayName}</span>`;
        btn.addEventListener("click", () => {
          this.selectAddress(s.displayName, s.latitude, s.longitude);
        });
        suggestionsDiv.appendChild(btn);
      });

      suggestionsDiv.style.display = "block";
    } catch (error) {
      console.error("Address search error:", error);
      suggestionsDiv.style.display = "none";
      suggestionsDiv.innerHTML = "";
    }
  }

  selectAddress(displayName, latitude, longitude) {
    const addressInput = document.getElementById(this.addressInputId);
    const suggestionsDiv = document.getElementById(this.suggestionsId);
    if (!addressInput) return;

    addressInput.value = displayName || "";

    const latEl = document.getElementById(this.latitudeInputId);
    const lonEl = document.getElementById(this.longitudeInputId);
    if (latEl) latEl.value = latitude ?? "";
    if (lonEl) lonEl.value = longitude ?? "";

    addressInput.classList.remove("is-invalid");

    if (suggestionsDiv) {
      suggestionsDiv.style.display = "none";
      suggestionsDiv.innerHTML = "";
    }
  }
}
