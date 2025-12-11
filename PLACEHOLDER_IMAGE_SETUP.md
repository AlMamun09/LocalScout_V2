# Placeholder Image Setup

## Required Image
You need to create a placeholder image for services that don't have an image uploaded yet.

### Image Specifications
- **Path**: `LocalScout.Web\wwwroot\images\placeholder-service.jpg`
- **Dimensions**: 800x600px (4:3 ratio)
- **Format**: JPG or PNG
- **Size**: < 100KB

### Option 1: Create Custom Placeholder
Create a simple image with:
- Light gray background (#f0f0f0)
- Icon or text in center (e.g., "Service Image")
- Professional look matching your brand

### Option 2: Use Free Placeholder Service
Download from:
- https://placehold.co/800x600/f0f0f0/999999?text=Service+Image
- https://via.placeholder.com/800x600/f0f0f0/999999?text=Service
- https://dummyimage.com/800x600/f0f0f0/999999&text=Service

### Option 3: Use Free Stock Photo
Download from:
- https://unsplash.com/ (search "construction" or "service")
- https://pixabay.com/ (search "tools" or "professional")
- https://pexels.com/ (search "handyman" or "service")

### Quick PowerShell Command to Download
```powershell
$url = "https://placehold.co/800x600/f0f0f0/999999/jpg?text=Service+Image"
$output = "LocalScout.Web\wwwroot\images\placeholder-service.jpg"
Invoke-WebRequest -Uri $url -OutFile $output
```

## Testing
After creating the image, verify it appears correctly:
1. Run the application
2. Navigate to home page
3. Check if services without images show the placeholder

## Alternative: Using Data URI
If you prefer, you can use a data URI instead of a file:

In `HomeController.cs`, change:
```csharp
return "/images/placeholder-service.jpg";
```

To:
```csharp
return "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='800' height='600'%3E%3Crect fill='%23f0f0f0' width='800' height='600'/%3E%3Ctext fill='%23999' font-family='sans-serif' font-size='24' x='50%25' y='50%25' text-anchor='middle' dy='.3em'%3EService Image%3C/text%3E%3C/svg%3E";
```
