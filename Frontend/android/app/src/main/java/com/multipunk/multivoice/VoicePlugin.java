package com.multipunk.multivoice;

import android.content.Intent;
import androidx.core.content.ContextCompat;
import com.getcapacitor.JSObject;
import com.getcapacitor.Plugin;
import com.getcapacitor.PluginCall;
import com.getcapacitor.PluginMethod;
import com.getcapacitor.annotation.CapacitorPlugin;

// JS side: registerPlugin('Voice') in useVoice
@CapacitorPlugin(name = "Voice")
public class VoicePlugin extends Plugin {
    private static VoicePlugin instance; // the notification buttons reach the page through it

    @Override
    public void load() {
        instance = this;
    }

    // starts VoiceService, or refreshes its notification: on mic permission and on every mic or sound toggle
    @PluginMethod
    public void start(PluginCall call) {
        Intent intent = new Intent(getContext(), VoiceService.class)
            .putExtra("micOn", call.getBoolean("micOn", true))
            .putExtra("deafened", call.getBoolean("deafened", false));
        ContextCompat.startForegroundService(getContext(), intent);
        call.resolve();
    }

    // "mic" or "deafen" from a notification button; the page toggles and sends its new state back through start()
    static void action(String action) {
        if (instance != null) instance.notifyListeners("action", new JSObject().put("action", action));
    }

    // closing the app drops the voice connection; opening it again reconnects
    static void disconnect() {
        if (instance != null && instance.getActivity() != null) instance.getActivity().finishAndRemoveTask();
    }
}
