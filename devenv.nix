{ pkgs, ... }:

{
  languages.javascript = {
    enable = true;
    package = pkgs.nodejs_24;
    npm.enable = true;
  };

  # the emulator's Qt only has X11, which fights a tiling Wayland WM over the window size; scrcpy shows it natively
  packages = [ pkgs.scrcpy ];

  # the Android app: SDK matches Frontend/android/variables.gradle, emulator for trying it out
  android = {
    enable = true;
    platforms.version = [ "36" ];
    buildTools.version = [ "35.0.0" "36.0.0" ];
    systemImageTypes = [ "google_apis" ];
    abis = [ "x86_64" ];
    emulator.enable = true;
    ndk.enable = false;
    android-studio.enable = false;
  };

  scripts.android-run.exec = ''
    set -e
    avdmanager list avd -c | grep -qx multivoice || echo no | avdmanager create avd -n multivoice -d pixel_7 -k "system-images;android-36;google_apis;x86_64"
    # the module puts build-tools' older libc++ on LD_LIBRARY_PATH, which crashes the emulator
    adb get-state >/dev/null 2>&1 || LD_LIBRARY_PATH="$ANDROID_HOME/emulator/lib64:$LD_LIBRARY_PATH" setsid -f emulator -avd multivoice -no-window >/dev/null 2>&1
    cd "$DEVENV_ROOT/Frontend"
    npm run android
    adb wait-for-device shell 'while [ "$(getprop sys.boot_completed)" != 1 ]; do sleep 1; done'
    (cd android && ./gradlew installDebug)
    adb shell am start -n com.multipunk.multivoice/.MainActivity
    pgrep -x scrcpy >/dev/null || setsid -f scrcpy --window-title MultiVoice >/dev/null 2>&1
  '';
}
