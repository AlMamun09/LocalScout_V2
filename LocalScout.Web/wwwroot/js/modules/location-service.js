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

    // Show loading state
    useLocationBtn.innerHTML =
      '<i class="fas fa-spinner fa-spin"></i> Getting Location...';
    useLocationBtn.disabled = true;

    navigator.geolocation.getCurrentPosition(
      async (position) => {
        const latitude = position.coords.latitude;
        const longitude = position.coords.longitude;
        const accuracy = position.coords.accuracy;

        console.log(
          `Location obtained: Lat ${latitude}, Lon ${longitude}, Accuracy: ${accuracy}m`
        );

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
          //addressInput.classList.add("is-valid");
        } catch (error) {
          console.error("Reverse geocoding error:", error);
          alert("Could not retrieve address. Please enter manually.");
          addressInput.focus();
        } finally {
          useLocationBtn.innerHTML = "Use My Location";
          useLocationBtn.disabled = false;
        }
      },
      (error) => {
        console.error("Geolocation error:", error);

        let errorMessage = "Unable to get your location. ";

        switch (error.code) {
          case error.PERMISSION_DENIED:
            errorMessage +=
              "You denied location access. Please enter your address manually or use autocomplete.";
            break;
          case error.POSITION_UNAVAILABLE:
            errorMessage += "Location information is unavailable.";
            break;
          case error.TIMEOUT:
            errorMessage += "The request timed out.";
            break;
          default:
            errorMessage += "An unknown error occurred.";
        }

        alert(errorMessage);
        const addressInputEl = document.getElementById(this.addressInputId);
        if (addressInputEl) addressInputEl.focus();

        useLocationBtn.innerHTML = "Use My Location";
        useLocationBtn.disabled = false;
      },
      {
        enableHighAccuracy: true,
        timeout: 10000,
        maximumAge: 0,
      }
    );
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
    //addressInput.classList.add("is-valid");

    if (suggestionsDiv) {
      suggestionsDiv.style.display = "none";
      suggestionsDiv.innerHTML = "";
    }
  }
}
