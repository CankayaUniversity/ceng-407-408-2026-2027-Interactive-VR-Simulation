mergeInto(LibraryManager.library, {
    NotifyWebUnityReady: function () {
        if (window.onUnitySimulationReady) {
            window.onUnitySimulationReady();
        }
    },

    SendStepProgressToWeb: function (jsonStringPtr) {
        var jsonPayload = UTF8ToString(jsonStringPtr);
        if (window.onUnityStepCompleted) {
            window.onUnityStepCompleted(jsonPayload);
        }
    },

    SendSimulationResultToWeb: function (jsonStringPtr) {
        var jsonPayload = UTF8ToString(jsonStringPtr);
        if (window.onUnitySimulationFinished) {
            window.onUnitySimulationFinished(jsonPayload);
        }
    }
});
