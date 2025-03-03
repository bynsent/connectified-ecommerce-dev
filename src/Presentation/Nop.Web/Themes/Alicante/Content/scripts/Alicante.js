(function ($, ssEx) {

    window.themeSettings = {
        themeBreakpoint: 1024,
        isAccordionMenu: true
    }

    $(document).ready(function () {
        var responsiveAppSettings = {
            isEnabled: false,
            themeBreakpoint: window.themeSettings.themeBreakpoint
        };
        ssEx.initResponsiveTheme(responsiveAppSettings);
    });

})(jQuery, sevenSpikesEx);