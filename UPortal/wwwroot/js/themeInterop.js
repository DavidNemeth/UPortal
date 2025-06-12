window.themeInterop = {
    applyTheme: function (themeName) {
        console.log("Applying theme: " + themeName);
        const body = document.body;
        // Remove any existing theme attributes/classes to avoid conflicts
        body.removeAttribute('theme'); // Used by some Fluent UI versions
        body.classList.remove('theme-light', 'theme-dark', 'theme-highcontrast'); // Example classes

        if (themeName === "light") {
            body.classList.add('theme-light');
            // For newer Fluent UI Blazor, it might be setting an attribute:
            // body.setAttribute('theme', 'light');
        } else if (themeName === "dark") {
            body.classList.add('theme-dark');
            // body.setAttribute('theme', 'dark');
        } else if (themeName === "highcontrast") {
            body.classList.add('theme-highcontrast');
            // body.setAttribute('theme', 'highcontrast');
        } else {
            // Default theme - ensure no specific theme class/attribute is set,
            // or apply a 'default' attribute if Fluent UI expects one.
            // For this example, we assume removing other themes reverts to default.
            // body.setAttribute('theme', 'default'); // or remove attribute
        }

        // Fluent UI Blazor specific way (if known, this is better)
        // This is a placeholder for actual Fluent UI theme switching JS if it exists
        // e.g., if (window.FluentUIBlazor && window.FluentUIBlazor.setTheme) {
        //    window.FluentUIBlazor.setTheme(themeName);
        // }

        // The most common way Fluent Web Components (which Fluent UI Blazor wraps)
        // handles theming is by setting attributes on the body or a DesignSystemProvider.
        // Let's assume for now it's a 'theme' attribute or specific 'theme-*' classes.
        // The FluentDesignTheme component in Fluent UI Blazor typically handles this by
        // reacting to its properties and applying necessary JS.
        // Our JS interop here is a more direct manipulation if the C# component isn't used or needs override.

        // For modern Fluent UI Blazor (v4+), theme is often controlled by DesignTheme on FluentLayout
        // This JS interop is a fallback or for themes not covered by DesignTheme component.
        // A more robust solution would involve checking Fluent UI Blazor documentation
        // for the official JavaScript API for theme switching.
        // For now, we'll assume setting an attribute is a common pattern.
        if (themeName && themeName !== "default") {
            document.body.setAttribute("theme", themeName);
        } else {
            document.body.removeAttribute("theme"); // Default theme
        }
    }
};
