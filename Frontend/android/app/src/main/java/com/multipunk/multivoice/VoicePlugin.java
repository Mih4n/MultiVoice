package com.multipunk.multivoice;

import android.content.Intent;
import androidx.core.content.ContextCompat;
import com.getcapacitor.Plugin;
import com.getcapacitor.PluginCall;
import com.getcapacitor.PluginMethod;
import com.getcapacitor.annotation.CapacitorPlugin;

// JS side: registerPlugin('Voice') in useVoice
@CapacitorPlugin(name = "Voice")
public class VoicePlugin extends Plugin {
    // starts VoiceService, or refreshes it once the mic permission is granted
    @PluginMethod
    public void start(PluginCall call) {
        ContextCompat.startForegroundService(getContext(), new Intent(getContext(), VoiceService.class));
        call.resolve();
    }
}
