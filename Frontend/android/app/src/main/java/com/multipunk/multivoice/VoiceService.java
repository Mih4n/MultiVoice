package com.multipunk.multivoice;

import android.Manifest;
import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.app.Service;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.content.pm.ServiceInfo;
import android.os.Build;
import android.os.IBinder;
import androidx.core.app.NotificationCompat;
import androidx.core.app.ServiceCompat;
import androidx.core.content.ContextCompat;

// a foreground service, like a call in Discord: while its notification is up,
// Android keeps the app alive in the background with the mic and sound on.
// The notification buttons toggle the mic and sound or disconnect
public class VoiceService extends Service {
    private static final String CHANNEL = "voice";

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        String action = intent != null ? intent.getAction() : null;
        if ("disconnect".equals(action)) {
            VoicePlugin.disconnect();
            ServiceCompat.stopForeground(this, ServiceCompat.STOP_FOREGROUND_REMOVE);
            stopSelf();
            return START_NOT_STICKY;
        }
        if (action != null) {
            VoicePlugin.action(action);
            return START_NOT_STICKY;
        }

        boolean micOn = intent == null || intent.getBooleanExtra("micOn", true);
        boolean deafened = intent != null && intent.getBooleanExtra("deafened", false);
        if (Build.VERSION.SDK_INT >= 26) {
            NotificationChannel channel = new NotificationChannel(CHANNEL, "Голосовая связь", NotificationManager.IMPORTANCE_LOW);
            getSystemService(NotificationManager.class).createNotificationChannel(channel);
        }
        PendingIntent open = PendingIntent.getActivity(this, 0, new Intent(this, MainActivity.class), PendingIntent.FLAG_IMMUTABLE);
        Notification notification = new NotificationCompat.Builder(this, CHANNEL)
            .setSmallIcon(android.R.drawable.ic_btn_speak_now)
            .setContentTitle("MultiVoice")
            .setContentText(deafened ? "Звук выключен" : micOn ? "Голосовая связь подключена" : "Микрофон выключен")
            .setContentIntent(open)
            .setOngoing(true)
            .addAction(0, micOn ? "Выкл. микрофон" : "Вкл. микрофон", button("mic", 1))
            .addAction(0, deafened ? "Вкл. звук" : "Выкл. звук", button("deafen", 2))
            .addAction(0, "Отключиться", button("disconnect", 3))
            .build();
        // the microphone type throws without the mic permission, so it's added once granted
        int type = ServiceInfo.FOREGROUND_SERVICE_TYPE_MEDIA_PLAYBACK;
        if (ContextCompat.checkSelfPermission(this, Manifest.permission.RECORD_AUDIO) == PackageManager.PERMISSION_GRANTED) {
            type |= ServiceInfo.FOREGROUND_SERVICE_TYPE_MICROPHONE;
        }
        ServiceCompat.startForeground(this, 1, notification, type);
        return START_NOT_STICKY;
    }

    private PendingIntent button(String action, int requestCode) {
        Intent intent = new Intent(this, VoiceService.class).setAction(action);
        return PendingIntent.getService(this, requestCode, intent, PendingIntent.FLAG_IMMUTABLE);
    }

    @Override
    public IBinder onBind(Intent intent) {
        return null;
    }
}
