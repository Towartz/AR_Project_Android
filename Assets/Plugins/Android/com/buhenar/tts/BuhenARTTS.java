package com.buhenar.tts;

import android.app.Activity;
import android.media.AudioManager;
import android.os.Bundle;
import android.speech.tts.TextToSpeech;
import android.util.Log;

import com.unity3d.player.UnityPlayer;

import java.util.Locale;

public final class BuhenARTTS {
    private static final String TAG = "BuhenARTTS";
    private static TextToSpeech tts;
    private static boolean ready;
    private static String pendingText;
    private static float speechRate = 0.88f;
    private static float pitch = 1.08f;

    private BuhenARTTS() {
    }

    public static void init(Activity activity) {
        final Activity targetActivity = activity != null ? activity : UnityPlayer.currentActivity;
        if (targetActivity == null) {
            Log.w(TAG, "init skipped: activity null");
            return;
        }

        targetActivity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                targetActivity.setVolumeControlStream(AudioManager.STREAM_MUSIC);
                if (tts != null) {
                    return;
                }

                ready = false;
                tts = new TextToSpeech(targetActivity.getApplicationContext(), new TextToSpeech.OnInitListener() {
                    @Override
                    public void onInit(int status) {
                        if (status != TextToSpeech.SUCCESS || tts == null) {
                            ready = false;
                            Log.w(TAG, "TextToSpeech init failed: " + status);
                            return;
                        }

                        int langResult = tts.setLanguage(new Locale("id", "ID"));
                        if (langResult == TextToSpeech.LANG_MISSING_DATA ||
                            langResult == TextToSpeech.LANG_NOT_SUPPORTED) {
                            Log.w(TAG, "Bahasa id-ID tidak tersedia, fallback ke locale perangkat");
                            tts.setLanguage(Locale.getDefault());
                        }

                        tts.setSpeechRate(speechRate);
                        tts.setPitch(pitch);
                        ready = true;
                        Log.i(TAG, "TextToSpeech ready");

                        if (pendingText != null && pendingText.trim().length() > 0) {
                            String text = pendingText;
                            pendingText = null;
                            speak(text);
                        }
                    }
                });
            }
        });
    }

    public static void setVoice(float rate, float voicePitch) {
        speechRate = rate;
        pitch = voicePitch;
        if (tts != null && ready) {
            tts.setSpeechRate(speechRate);
            tts.setPitch(pitch);
        }
    }

    public static void speak(String text) {
        final String safeText = text == null ? "" : text.trim();
        if (safeText.length() == 0) {
            return;
        }

        final Activity targetActivity = UnityPlayer.currentActivity;
        if (targetActivity == null) {
            pendingText = safeText;
            Log.w(TAG, "speak queued: activity null");
            return;
        }

        targetActivity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                targetActivity.setVolumeControlStream(AudioManager.STREAM_MUSIC);
                if (tts == null || !ready) {
                    pendingText = safeText;
                    init(targetActivity);
                    Log.i(TAG, "speak queued before ready: " + safeText);
                    return;
                }

                Bundle params = new Bundle();
                params.putInt(TextToSpeech.Engine.KEY_PARAM_STREAM, AudioManager.STREAM_MUSIC);
                int result = tts.speak(
                    safeText,
                    TextToSpeech.QUEUE_FLUSH,
                    params,
                    "buhenar_" + System.nanoTime()
                );
                Log.i(TAG, "speak result=" + result + " text=" + safeText);
            }
        });
    }

    public static void stop() {
        pendingText = null;
        if (tts != null) {
            tts.stop();
        }
    }

    public static void shutdown() {
        pendingText = null;
        ready = false;
        if (tts != null) {
            tts.stop();
            tts.shutdown();
            tts = null;
        }
    }
}
