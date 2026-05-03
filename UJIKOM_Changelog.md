# UJIKOM Changelog

Dokumen ini merangkum pekerjaan yang sudah dilakukan pada project `AR_UJIKOM_PROJECT`, mencakup pekerjaan sebelumnya dan yang sedang dikerjakan saat ini.

---

## 2026-04-21 — Improvement via Claude + Desktop Commander MCP

### AppSession.cs
- Tambah **persistent stats** via `PlayerPrefs`: high score, best streak, total quiz played, total correct all-time, total scans all-time.
- Tambah property `LastQuizIsNewHighScore`, `LastQuizPercentage`, derived stats.
- Tambah method `UpdateStreak(int)` dan `ResetAllPersistentData()` (untuk debug/settings reset).
- High score dibandingkan berdasarkan **persentase** (bukan nilai absolut) agar fair bila jumlah soal berubah.

### QuizManager.cs
- Tambah **timer per soal** (`questionTimeLimitSeconds`, `Slider` + `Text` timer UI, warning color saat < 30%).
- Tambah **streak tracking** per sesi (`currentStreak`, `sessionBestStreak`) + `UpdateStreak()` ke AppSession.
- Tambah **streak UI label** (`streakText`) dengan teks bertingkat (2x / 3x / 5x+).
- Timeout otomatis highlight jawaban benar tanpa menambah skor.
- `enableTimer` flag — timer bisa dinonaktifkan via Inspector.
- Refactor kode lebih ringkas dan konsisten.

### ResultSceneController.cs
- Tambah **stats panel**: high score, best streak, total quiz played, total scans all-time.
- Tambah `newRecordLabel` — muncul otomatis bila sesi ini cetak high score baru.
- Tambah **score count-up animation** (ease-out cubic, durasi konfigurasikan via Inspector).
- Feedback dan CTA lebih granular (5 level: <50 / 50-70 / 70-90 / 90+ / new record).

### SceneNavigationController.cs
- Tambah **scene history stack** untuk Android back button support.
- `GoBack()` — pop stack dan navigasi ke scene sebelumnya.
- `OpenMainMenu()` — selalu clear history (root navigation).
- `LoadWithFade()` — fade out / in antar scene via canvas overlay (`DontDestroyOnLoad`).
- `Update()` handle `KeyCode.Escape` untuk back button Android.

### UIOverlayController.cs
- Tambah **panel slide-in animation** dengan `CanvasGroup` alpha + `RectTransform` offset (ease smooth step).
- `SetInfoPanelVisible(bool)` dengan coroutine animasi — menggantikan `SetActive` langsung.
- `ToggleInfoPanel()` kini pakai animasi.
- Refactor duplikasi kode, field lebih terorganisir per Header.

### SplashController.cs
- Tambah **logo fade-in / fade-out** via `CanvasGroup`.
- Tambah **version label** otomatis dari `Application.version`.
- Tambah **skip touch** — tap/key manapun skip splash langsung ke MainMenu.

### ProgressBadgeUI.cs *(script baru)*
- Widget reusable untuk menampilkan progress "X / Y materi ditemukan".
- Support `Slider` progress bar + `Image` badge icon (hijau kalau complete).
- `refreshEveryFrame` flag untuk update real-time di AR scene.
- Bisa dipasang di scene manapun dengan reference ke `MaterialContentController`.

---


## Sebelumnya

### Setup Android dan Toolchain
- Mengecek konfigurasi `OpenJDK`, `Android SDK`, dan `Android NDK` pada sistem serta preferensi Unity.
- Memastikan Unity memakai kombinasi toolchain yang stabil untuk build Android:
  - `JDK`: Embedded OpenJDK bawaan Unity
  - `SDK`: `/opt/android-sdk`
  - `NDK`: `/opt/android-sdk/ndk/27.2.12479018`
- Memasang `Android Build Support` untuk Unity `6000.4.1f1`.
- Menjalankan build test Android dan memverifikasi APK berhasil dibuat.

### Implementasi Aplikasi AR Berdasarkan Instruksi
- Membaca instruksi dari `/home/twentyone/Downloads/UNITY_TUTORIAL/AR_project.md`.
- Membuat dan/atau meregenerasi struktur scene aplikasi:
  - `SplashScene`
  - `MainMenuScene`
  - `GuideScene`
  - `MaterialSelectScene`
  - `ARScanScene`
  - `QuizScene`
  - `ResultScene`
  - `AboutScene`
- Menambahkan generator/editor tool untuk mempermudah regenerasi scene dan konten project.
- Menyiapkan data materi, quiz, dan alur navigasi aplikasi.

### Build dan Deploy ke Android
- Membuat helper build Android headless untuk Unity.
- Menjalankan build APK Android beberapa kali untuk validasi.
- Menginstall APK hasil build ke perangkat Android melalui ADB.

### Migrasi dari AR Foundation ke Vuforia
- Mengevaluasi keterbatasan kompatibilitas `AR Foundation` di Android karena tetap bergantung pada `ARCore`.
- Mengimport paket `Vuforia Engine` ke project.
- Mengubah runtime AR dari `AR Foundation` ke `Vuforia`.
- Mengganti alur image tracking ke sistem observer/target Vuforia.
- Membersihkan dependensi lama `AR Foundation`, `ARCore`, dan `ARKit` dari project setelah migrasi stabil.
- Mengosongkan loader XR yang tidak lagi dipakai.

### Barcode Support untuk Vuforia
- Menambahkan alternatif scanning berbasis barcode agar demo tidak hanya bergantung pada image target.
- Mengaktifkan dukungan beberapa format barcode, termasuk:
  - `QRCODE`
  - `MICROQRCODE`
  - `CODE128`
  - `CODE39`
  - `DATAMATRIX`
  - `PDF417`
  - `AZTEC`
- Menambahkan pemetaan payload barcode ke materi konten.
- Membuat sample barcode untuk pengujian awal.

### UI/UX dan Perbaikan Tampilan
- Mengevaluasi layout mobile yang sebelumnya terlalu sempit dan tidak terbaca.
- Merombak tampilan `MaterialSelectScene` menjadi lebih mobile-first.
- Membesarkan elemen interaksi dan merapikan struktur visual agar lebih layak di layar HP.

## Saat Ini

### Integrasi Asset Demo dari Blender
- Menelusuri lokasi instalasi Blender dan asset terkait pada sistem.
- Mengonfirmasi Blender tersedia melalui instalasi Flatpak di area sistem (`/var/lib/flatpak/...`).
- Menggunakan Blender dalam mode batch untuk membuat object demo sederhana langsung dari Blender, bukan placeholder Unity biasa.

### Object Demo yang Sudah Dibuat
- `blend_cube`
- `blend_roundcube`
- `blend_pyramid`
- `blend_torus`
- `blend_cylinder`

### Pipeline Asset Demo yang Sudah Ditambahkan
- Membuat script Blender untuk export object demo ke format FBX.
- Mengimport hasil export ke Unity.
- Membuat material demo untuk tiap object.
- Membuat prefab demo untuk tiap object.
- Menambahkan komponen animasi rotasi sederhana pada object demo agar lebih hidup saat muncul di AR.
- Membuat/update `ScriptableObject` untuk object demo agar bisa dipakai oleh sistem konten project.
- Menambahkan object demo tersebut ke content library project.

### Dukungan Barcode untuk Object Demo
- Menambahkan payload debug/global agar barcode bisa langsung memunculkan object demo lintas kategori.
- Payload yang disiapkan saat ini mencakup:
  - `cube`
  - `cubes`
  - `debugcube`
  - `blendcube`
  - `blendroundcube`
  - `blendpyramid`
  - `blendtorus`
  - `blendcylinder`

### Status Verifikasi Saat Ini
- Asset Blender demo sudah berhasil dibuat dan masuk ke project.
- Prefab, material, dan `ScriptableObject` untuk object demo sudah terbentuk.
- Device Android saat ini terdeteksi via ADB.
- `qrencode` tersedia di sistem, sehingga barcode test tambahan bisa digenerate.

## Catatan
- Ada beberapa iterasi build Unity yang sebelumnya sempat lama atau macet; proses batch Unity yang menggantung sudah diidentifikasi dan dihentikan agar pekerjaan bisa lanjut.
- Regenerasi scene full-project lewat satu entry point masih perlu diperlakukan hati-hati; beberapa bagian lebih stabil saat dijalankan per-scene atau per-tool.
- Langkah berikutnya yang paling masuk akal adalah:
  1. generate barcode final untuk object Blender demo,
  2. build ulang APK Android,
  3. install APK terbaru ke HP,
  4. verifikasi scan barcode dan kemunculan object demo di AR.

## Update Terbaru: Fix Marker Scan Vuforia
- Ditemukan bahwa QR/barcode polos terlalu lemah untuk dipakai sebagai `Vuforia image target` yang stabil untuk memunculkan object 3D.
- Sistem demo Blender diubah agar tidak lagi mengandalkan QR polos sebagai marker utama.
- Dibuat marker card baru yang tetap memuat QR di tengah, tetapi diberi elemen visual asimetris tambahan agar feature tracking Vuforia lebih kuat.
- Marker card baru digenerate otomatis ke folder `Assets/Art/BarcodeMarkers/`.
- `ScriptableObject` demo `blend_cube`, `blend_roundcube`, `blend_pyramid`, `blend_torus`, dan `blend_cylinder` sekarang mengarah ke marker card baru tersebut sebagai `referenceImageTexture`.
- APK Android dibuild ulang setelah perubahan marker selesai.
- APK hasil build terbaru berhasil diinstall ke device Android melalui ADB.
- Marker card final juga disalin ke folder `Pictures/UJIKOM_Barcodes_Markers/` agar mudah dibuka dan discan saat test.

## Status Verifikasi Terbaru
- Build Android terbaru: `Success`
- Output APK: `Builds/Android/TestBuild.apk`
- Marker yang sekarang harus discan:
  - `blend_cube_marker.png`
  - `blend_roundcube_marker.png`
  - `blend_pyramid_marker.png`
  - `blend_torus_marker.png`
  - `blend_cylinder_marker.png`
- File QR/barcode lama di `Pictures/UJIKOM_Barcodes/` tidak lagi menjadi acuan utama untuk test tracking 3D demo Blender.

## File dan Area Project yang Sudah Banyak Tersentuh
- `Assets/Editor/ARtiGrafProjectBuilder.cs`
- `Assets/Editor/BuildAndroidTest.cs`
- `Assets/Editor/GenerateBlenderDemoAssets.cs`
- `Assets/Scripts/AR/ARImageTrackingController.cs`
- `Assets/Scripts/AR/MaterialContentController.cs`
- `Assets/Scripts/AR/DemoSpinObject.cs`
- `Assets/Scripts/Core/AppSession.cs`
- `Assets/Scripts/Core/SceneNavigationController.cs`
- `Assets/Scripts/UI/UIOverlayController.cs`
- `Assets/Scripts/Quiz/QuizManager.cs`
- `Assets/Scripts/Quiz/ResultSceneController.cs`
- `Tools/Blender/export_demo_shapes.py`
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `Assets/Scenes/*`

## Update Terbaru: Content Library Dilengkapi
- Content library diperluas dari 13 item menjadi 23 item aktif.
- Materi inti tipografi sekarang mencakup:
  - `typo_serif`
  - `typo_sans`
  - `typo_script`
  - `typo_display`
  - `typo_monospace`
  - `typo_slab`
  - `typo_geometric`
  - `typo_blackletter`
- Materi inti warna sekarang mencakup:
  - `color_warm`
  - `color_cool`
  - `color_contrast`
  - `color_harmony`
  - `color_primary`
  - `color_secondary`
  - `color_pastel`
  - `color_earth`
- `MaterialSelectScene` diperbarui agar jumlah materi per kategori mengikuti generator terbaru.
- Generator reference image diperkuat dengan pola yang lebih unik per item supaya target scan tidak terasa terlalu mirip satu sama lain.
- Generator prefab inti sekarang mendukung `prefabTemplateName`, sehingga variasi materi baru dapat memakai bentuk dasar yang sama dengan warna/identitas berbeda tanpa bentrok nama prefab.

## Update Terbaru: Integrasi Apple Blender
- Asset eksternal `/home/twentyone/Documents/UJIKOM_PROJECT/Apple_Model.blend` diaudit dan tervalidasi berisi mesh `Apel` dan `pucuk`.
- Ditambahkan script reusable `Tools/Blender/export_open_blend_meshes.py` untuk export asset `.blend` eksternal ke FBX dari Blender batch mode.
- Model Apple diexport ke:
  - `Assets/Imported/BlenderDemo/blend_apple.fbx`
- Demo asset Apple dilengkapi:
  - material `Assets/Materials/BlenderDemo/blend_apple.mat`
  - prefab `Assets/Prefabs/BlenderDemo/blend_apple.prefab`
  - `ScriptableObject` `Assets/ScriptableObjects/blend_apple.asset`
  - barcode target `Assets/Art/BarcodeTargets/blend_apple.png`
  - marker card `Assets/Art/BarcodeMarkers/blend_apple_marker.png`
- Payload debug/global diperluas agar `blend_apple` bisa dipindai lintas mode seperti marker demo Blender lain.

## Update Terbaru: Refresh dan Validasi Build
- `Tools/ARtiGraf/Refresh Content Library` berhasil dijalankan ulang setelah pipeline import barcode baru diperbaiki supaya texture barcode selalu `readable`.
- `ARScanScene` dan `MaterialSelectScene` diregenerasi ulang agar library baru langsung terhubung ke scene.
- Build Android verifikasi terbaru berhasil selesai:
  - output APK: `Builds/Android/TestBuild.apk`
  - status: `Success`
  - durasi build: sekitar `00:01:30`
- Saat build headless di Linux, Vuforia sempat melempar `DllNotFoundException: VuforiaEngine.dll` pada preprocess editor-side, tetapi proses build Android tetap selesai dan APK tetap berhasil dihasilkan.

## Update Terbaru: Fix Marker Switch dan Marker Mismatch
- `ARImageTrackingController` diperbarui supaya image target lama tidak terlalu lengket saat status turun ke `LIMITED` atau `EXTENDED_TRACKED`.
- Ditambahkan grace period singkat `imageTargetLostGraceSeconds` agar perpindahan marker tetap halus, tetapi object lama cepat dilepas saat marker baru masuk.
- Mode stabilisasi tetap dipertahankan, namun tracking aktif sekarang hanya dianggap valid penuh saat target benar-benar `TRACKED` untuk image target.
- Generator marker demo Blender (`GenerateBlenderDemoAssets`) diperkuat dengan pola hash unik per marker.
- Marker `blend_cube`, `blend_roundcube`, `blend_apple`, `blend_pyramid`, `blend_torus`, dan `blend_cylinder` sekarang punya layout visual yang jauh lebih berbeda, bukan hanya beda warna.
- Hal ini ditujukan untuk mengurangi kasus:
  - marker Apple memunculkan object Pyramid
  - marker berganti tetapi object tetap tertahan di Apple
  - target salah klasifikasi karena fitur visual antar marker terlalu mirip
- Marker baru diregenerate ulang, disalin ke `Pictures/UJIKOM_Barcodes_Markers`, lalu APK Android dibuild ulang dan diinstall ke HP.
- Build Android hasil fix berhasil:
  - output APK: `Builds/Android/TestBuild.apk`
  - status: `Success`
  - install device: `Success`

## Update Terbaru: Fix Apple Fallback dan Interaksi Sentuh
- Resolver payload di `MaterialContentLibrary` diperketat dengan dua tahap:
  - pencocokan exact lebih dulu ke `id` dan `referenceImageName`
  - pencocokan longgar (`title`, `subtitle`, `objectType`) hanya dipakai sebagai fallback terakhir
- Perubahan ini ditujukan untuk mengurangi kasus marker `blend_apple` malah memunculkan object demo lain seperti cube biru.
- `ARImageTrackingController` diperbarui agar konten demo `blend_*` lebih mengutamakan alur barcode dibanding image target runtime.
- Untuk marker demo Blender, object barcode sekarang bisa di-anchor langsung ke marker hasil scan, bukan hanya ke preview camera.
- State object aktif dan payload barcode aktif sekarang disinkronkan lebih ketat agar perpindahan marker lebih konsisten saat barcode berubah.
- Ditambahkan interaksi sentuh di Android:
  - swipe satu jari untuk rotate object aktif
  - pinch dua jari untuk scale object aktif
  - `DemoSpinObject` akan dimatikan otomatis saat object pertama kali disentuh agar kontrol manual tidak melawan auto-rotate
- Implementasi ini ditujukan agar object demo AR terasa lebih interaktif saat pengujian di HP, tanpa perlu komponen UI tambahan.
- Build Android verifikasi terbaru berhasil selesai:
  - output APK: `Builds/Android/TestBuild.apk`
  - ukuran APK: sekitar `65 MB`
  - status build: `Success`
  - durasi build: sekitar `00:06:56`
- APK hasil patch sudah diinstall ulang ke device lewat `adb install -r`.
- Aplikasi berhasil dijalankan kembali di device setelah build.

## Update Terbaru: Fix Regresi Barcode Hijau dan Mismatch Demo
- Kasus barcode scan yang kembali menampilkan kotak hijau besar seperti overlay Vuforia ditangani dengan mematikan `BarcodeOutlineBehaviour` default pada alur barcode demo.
- `ARImageTrackingController` sekarang tetap membuat `BarcodeBehaviour`, tetapi outline hijaunya tidak lagi ditampilkan saat marker QR dibaca.
- Anchor barcode ke marker juga diperbaiki agar tidak lagi mewarisi `lossyScale` observer barcode.
- Untuk content demo hasil scan barcode, anchor barcode sekarang memakai `Vector3.one` supaya object tidak membesar liar saat QR berhasil terdeteksi.
- Resolver scan payload di `MaterialContentLibrary` dipersempit menjadi `exact match` untuk `id` dan `referenceImageName`.
- Fallback fuzzy ke `title`, `subtitle`, atau `objectType` dihapus dari jalur scan barcode karena ini terbukti bisa membuat marker Apple lompat ke object lain seperti cube debug.
- Alias lama `cube` dan `debugcube` tetap dipetakan ke payload `cubes`, jadi barcode debug lama tidak ikut rusak.
- Build Android verifikasi hasil fix regresi berhasil:
  - output APK: `Builds/Android/TestBuild.apk`
  - status build: `Success`
  - durasi build: sekitar `00:01:08`
- APK hasil fix regresi sudah diinstall ulang ke HP dan aplikasi berhasil dijalankan kembali.

## Update Terbaru: Balikkan Demo Mobile ke Image Target
- Dari gejala terbaru, jalur yang sehat ternyata adalah saat marker demo Blender dibaca sebagai `image target` biasa, bukan saat masuk ke mode barcode Vuforia.
- `ARImageTrackingController` sekarang tidak lagi memprioritaskan `BarcodeBehaviour` untuk konten demo `blend_*` di Android/mobile.
- Akibatnya, marker seperti `blend_apple_marker` kembali mengikuti alur tracking image target yang sebelumnya berhasil memunculkan object Apple tanpa watermark Vuforia.
- Scene `ARScanScene` juga diserialisasi ulang agar flag runtime konsisten:
  - `preferBarcodeTrackingForDemoMarkers: 0`
  - `showBarcodeOutline: 0`
- Build Android verifikasi terbaru berhasil:
  - output APK: `Builds/Android/TestBuild.apk`
  - status build: `Success`
  - durasi build: sekitar `00:01:10`
- APK hasil patch ini sudah diinstall ulang ke device dan aplikasi berhasil dijalankan kembali untuk retest marker Apple.

## Update Terbaru: Autofocus, Stabilitas Object, dan Gesture Sentuh
- `ARImageTrackingController` diperkuat dengan runtime autofocus Vuforia saat engine mulai dan saat target berhasil didapat lagi.
- Ditambahkan `tap-to-refocus` di Android:
  - tap singkat pada area kamera akan meminta autofocus ulang
  - setelah trigger autofocus, mode continuous autofocus dipulihkan kembali otomatis
- Stabilisasi anchor ditingkatkan dengan deadzone anti-jitter untuk posisi dan rotasi, sehingga object tidak terlalu mudah bergetar saat marker sebenarnya masih diam.
- Tuning scene `ARScanScene` ikut diperbarui:
  - smoothing posisi/rotasi dibuat sedikit lebih halus
  - parameter autofocus dan anti-jitter diserialisasi ke scene
- Interaksi sentuh object demo diperluas:
  - satu jari drag untuk rotate
  - dua jari pinch untuk scale
  - dua jari drag untuk menggeser object di area marker
  - touch pada UI tidak lagi ikut memutar object karena gesture AR sekarang mengabaikan sentuhan di atas elemen UI
- Build Android verifikasi hasil patch interaksi dan stabilisasi berhasil:
  - output APK: `Builds/Android/TestBuild.apk`
  - status build: `Success`
  - durasi build: sekitar `00:02:29`
- APK hasil patch ini sudah diinstall ulang ke device dan aplikasi berhasil dijalankan kembali.
- Ada patch tambahan kecil untuk memastikan autofocus runtime tetap dijalankan juga jika `VuforiaApplication` sudah lebih dulu berstatus `running` sebelum callback `OnVuforiaStarted`.
- Build Android final sesudah patch startup autofocus juga berhasil:
  - output APK: `Builds/Android/TestBuild.apk`
  - status build: `Success`
  - durasi build: sekitar `00:01:04`
- APK final sesudah patch startup autofocus sudah diinstall ulang ke device dan aplikasi berhasil dijalankan kembali.

## Update Terbaru: Fix Interaksi Sentuh di Build Android
- Akar masalah interaksi disentuh ulang di `ARImageTrackingController`:
  - project memakai `Input System` baru
  - gesture AR sebelumnya masih mengandalkan jalur touch lama (`UnityEngine.Input`) untuk sebagian logic
- Jalur sentuh sekarang dibaca lewat `EnhancedTouch` saat `Input System` baru aktif, lalu baru fallback ke touch legacy bila memang diperlukan.
- Logika gesture dua jari diperbaiki agar memakai jumlah touch yang sama dengan buffer input runtime, bukan lagi `Input.touchCount` langsung.
- Ini penting karena sebelumnya pinch/drag dua jari bisa tetap mati walaupun satu jari terlihat seolah sudah didukung.
- Penyaring sentuhan di atas UI juga diperketat:
  - sentuhan hanya diblok saat mengenai elemen UI yang benar-benar interaktif seperti `Button`/`Selectable` atau `ScrollRect`
  - elemen dekoratif overlay tidak lagi ikut menelan gesture AR
- Setting scene diverifikasi masih benar:
  - `enableTouchInteraction: 1`
  - `enableTapToRefocus: 1`
  - `configureRuntimeCameraFocus: 1`
- Build Android verifikasi terbaru berhasil:
  - output APK: `Builds/Android/TestBuild.apk`
  - ukuran APK: sekitar `67.2 MB`
  - status build: `Success`
  - durasi build: sekitar `00:04:22`
- APK hasil fix interaksi terbaru sudah diinstall ulang ke device lewat `adb install -r` dan aplikasi berhasil dijalankan kembali.

## Update Terbaru: Perbaikan Anchor dan Rotasi Object AR
- Akar masalah rotasi dan anchor yang terasa aneh ada pada transform object yang sebelumnya menumpuk beberapa tanggung jawab di node yang sama:
  - posisi anchor marker
  - rotasi gesture user
  - scale gesture user
  - offset internal prefab Blender
- `ARImageTrackingController` sekarang memisahkan hierarchy runtime menjadi dua layer:
  - `placementRoot` untuk posisi anchor dan offset terhadap marker
  - `interactionRoot` untuk rotasi dan scale hasil gesture user
- Saat prefab dimunculkan, bounds renderer sekarang dinormalisasi otomatis supaya:
  - titik bawah object duduk lebih dekat ke marker
  - pusat X/Z object lebih rapi
  - prefab dengan pivot internal Blender yang kurang konsisten tidak lagi mudah tampak miring atau melayang
- Rotasi gesture satu jari sekarang tidak lagi mengakumulasi orientasi secara liar terhadap root lama.
- Rotasi user disimpan sebagai yaw/pitch terkontrol dengan clamp pitch, sehingga object terasa lebih stabil dan tidak cepat "aneh" saat disentuh berkali-kali.
- Offset default content terhadap image target diturunkan dari `0.035` menjadi `0.012` agar object tidak terasa terlalu mengambang di atas marker.
- Scene `ARScanScene` ikut diperbarui agar tuning baru aktif langsung di build:
  - `normalizeSpawnedContentPivot: 1`
  - `interactionPitchClampDegrees: 55`
  - `imageTargetContentOffset.y: 0.012`
- Build Android verifikasi hasil patch anchor/rotasi berhasil:
  - output APK: `Builds/Android/TestBuild.apk`
  - status build: `Success`
  - durasi build: sekitar `00:05:17`
- APK hasil patch anchor/rotasi sudah diinstall ulang ke device lewat `adb install -r`.
- Aplikasi berhasil dijalankan kembali di device setelah install build terbaru ini.

## Update Terbaru: Tooling Maintenance Konten AR
- Untuk mempermudah maintenance project, alur edit konten AR sekarang tidak lagi sepenuhnya manual.
- Ditambahkan utility runtime bersama:
  - `MaterialContentKeyUtility`
  - dipakai untuk normalisasi key scan secara konsisten antara runtime dan tooling editor
- `MaterialContentData` sekarang punya helper property tambahan agar validator dan editor tooling bisa membaca status asset lebih jelas:
  - `NormalizedId`
  - `NormalizedReferenceImageName`
  - `HasReferenceImage`
  - `HasPrefab`
  - `IsBarcodeOnly`
- `MaterialContentLibrary` diperluas dengan API maintenance-friendly:
  - `Count`
  - `Contains(...)`
  - `ReplaceItems(...)`
- Ditambahkan tooling editor baru:
  - `Assets/Editor/ARtiGrafContentMaintenance.cs`
  - `Assets/Editor/MaterialContentDataEditor.cs`
- Menu maintenance baru di Unity:
  - `Tools/ARtiGraf/Content/Sync Library From ScriptableObjects`
  - `Tools/ARtiGraf/Content/Validate Content Setup`
  - `Tools/ARtiGraf/Content/Sync Library And Validate`
  - `Assets/ARtiGraf/Content/Auto Configure Selected Content`
- Fungsi tooling baru:
  - sinkronisasi otomatis `ARtiGrafContentLibrary.asset` dari seluruh `MaterialContentData`
  - validasi konten AR untuk cek error umum sebelum build
  - auto-config asset baru dari naming convention project
  - helper Inspector langsung di asset `MaterialContentData`
- Validator maintenance menghasilkan report markdown:
  - `UJIKOM_Content_Report.md`
- Verifikasi compile editor tooling berhasil lewat Unity batchmode.
- Verifikasi validator maintenance juga berhasil:
  - checked content: `23`
  - errors: `0`
  - warnings: `0`

## Update Terbaru: Content Dashboard di Unity Editor
- Ditambahkan `Assets/Editor/ARtiGrafContentDashboardWindow.cs`.
- Dashboard dibuka dari menu:
  - `Tools/ARtiGraf/Content/Open Dashboard`
- Fungsi dashboard:
  - melihat seluruh `MaterialContentData` dari satu panel
  - filter cepat per kategori atau status:
    - `All`
    - `Typography`
    - `Color`
    - `Barcode`
    - `Blend`
    - `Issues`
  - search berdasarkan `id`, nama asset, prefab, marker, atau barcode
  - melihat health status per content:
    - ada di library atau tidak
    - prefab terhubung atau tidak
    - reference image ada atau tidak
    - barcode target ada atau tidak
    - duplicate scan key atau tidak
  - aksi cepat per content:
    - `Select Asset`
    - `Auto Configure`
    - `Ping Prefab`
    - `Ping Marker`
    - `Ping Barcode`
  - aksi toolbar global:
    - `Refresh`
    - `Sync Library`
    - `Validate`
    - `Sync + Validate`
    - `Reveal Report`
- Dashboard ini dibuat supaya maintenance harian tidak perlu lagi bolak-balik buka banyak folder atau menebak asset mana yang putus.
- Verifikasi compile sesudah penambahan dashboard berhasil lewat Unity batchmode.

## Update Terbaru: Create New Content Wizard
- Ditambahkan `Assets/Editor/ARtiGrafCreateContentWizardWindow.cs`.
- Wizard dibuka dari menu:
  - `Tools/ARtiGraf/Content/Create New Content`
  - `Assets/ARtiGraf/Content/Create New Content From Selection`
- Fungsi wizard:
  - membuat atau update `MaterialContentData` dari satu form editor
  - prefill otomatis dari selection aktif:
    - `MaterialContentData`
    - `GameObject`
    - `Texture2D`
  - auto-sanitize `Content Id` agar konsisten dengan naming project
  - auto-link prefab, reference image, dan barcode berdasarkan `id`
  - opsi auto-sync library setelah create
  - opsi auto-validate setelah create
  - opsi auto-select asset hasil create
- Dashboard juga dihubungkan ke wizard lewat tombol:
  - `New Content`
- Warning compile editor karena field `title` bentrok dengan `EditorWindow.title` sudah dibersihkan.
- Verifikasi compile sesudah penambahan wizard berhasil lewat Unity batchmode.
- Hasil akhirnya:
  - tambah content baru sekarang tidak harus setup manual dari banyak panel Inspector
  - maintenance harian bisa lewat 3 jalur:
    - Inspector helper
    - Dashboard
    - Create New Content Wizard

## Update Terbaru: Android Build Window dengan Tombol Opsi
- Ditambahkan helper deploy Android:
  - `Assets/Editor/ARtiGrafAndroidBuildTools.cs`
- Ditambahkan window editor khusus:
  - `Assets/Editor/ARtiGrafAndroidBuildWindow.cs`
- Window dibuka dari menu:
  - `Tools/ARtiGraf/Build/Open Android Build Window`
- Dashboard konten juga dihubungkan ke build window lewat tombol:
  - `Android Build`
- Opsi tombol utama di window:
  - `Build APK`
  - `Build + Install`
  - `Build + Install + Launch`
- Opsi tombol tambahan:
  - `Install Last APK`
  - `Launch Installed App`
  - `Reveal APK`
  - `Refresh`
  - `Clear Log`
- Build pipeline batch lama tetap dipertahankan:
  - `BuildAndroidTest.PerformBuild()` sekarang memakai helper build yang sama
- Build window juga menampilkan status:
  - package identifier
  - path `adb`
  - path APK output
  - device yang terdeteksi
  - log aktivitas deploy
- Satu optimasi tambahan:
  - polling device tidak dilakukan tiap repaint UI lagi, jadi window lebih ringan untuk maintenance harian

## Update Terbaru: Prioritas Image Target atas Barcode untuk Cegah Salah Object
- `Assets/Scripts/AR/ARImageTrackingController.cs` diperkuat agar barcode tidak lagi mudah menimpa object dari marker image target yang sudah aktif.
- Ditambahkan guard baru:
  - `preferTrackedImageTargetsOverBarcodeResults`
  - `requireTrackedStatusForBarcodeSwitch`
- Perubahan perilaku:
  - kalau image target aktif dan barcode membaca payload lain, payload barcode itu diabaikan
  - kalau image target aktif dan barcode membaca payload yang sama, image target tetap diprioritaskan agar tidak spawn ganda
  - perpindahan object dari barcode baru sekarang menunggu status `TRACKED` penuh, tidak langsung ganti saat status masih `LIMITED`
- Efek yang dituju:
  - kasus marker Apple tiba-tiba berubah ke cube biru jauh berkurang
  - pergantian marker lebih stabil
  - barcode liar di background tidak mudah merebut object aktif

## Update Terbaru: Preview Mode Otomatis dan Gesture Lebih Natural
- `ARImageTrackingController` sekarang punya preview mode otomatis saat tidak ada marker aktif.
- Preview mode memakai object terakhir yang dilihat atau content pertama yang cocok dengan kategori aktif.
- Object preview dirender di anchor depan kamera sebagai mode tampilan tanpa perlu marker aktif terus-menerus.
- Overlay UI juga ditingkatkan:
  - `UIOverlayController` sekarang membuat backdrop gelap runtime saat preview mode aktif
  - panel materi tetap tampil, tapi kamera terasa lebih seperti studio preview
- Interaksi disentuh ulang supaya lebih natural:
  - rotasi default sekarang fokus ke yaw, bukan pitch liar
  - pan object preview bergerak di bidang layar, bukan terasa muter di orbit marker
  - scale tetap lewat pinch
- Efek yang dituju:
  - object lebih gampang diputar
  - geser object terasa lebih masuk akal
  - ada mode visual untuk lihat model tanpa harus terus mengunci marker

## Update Terbaru: Guard Vuforia Linux Editor
- Ditambahkan `Assets/Scripts/AR/VuforiaEditorLinuxGuard.cs`.
- Komponen guard ini dipasang langsung di `Main Camera` pada `Assets/Scenes/ARScanScene.unity`.
- Perilaku barunya:
  - saat Play Mode di Linux editor, `VuforiaBehaviour` otomatis dimatikan sebelum runtime Vuforia dipakai
  - `DefaultInitializationErrorHandler` ikut dimatikan agar dialog error webcam/Vuforia native tidak muncul terus
  - scene otomatis jatuh ke preview mode tanpa kamera
- Efek yang dituju:
  - error `Could not parse webcam profile` di editor Linux berhenti mengganggu
  - error `DllNotFoundException: VuforiaEngine.dll` tidak lagi memblok play mode editor
  - scan nyata tetap dilakukan lewat build Android, bukan lewat editor Linux

## Update Terbaru: Editor Webcam Preview Mode
- `ARImageTrackingController` sekarang bisa menyalakan feed `WebCamTexture` sebagai background saat Play Mode di Linux editor.
- Preview object tetap memakai jalur preview internal, jadi tidak tergantung runtime Vuforia Linux.
- Kontrol desktop ditambahkan untuk maintenance cepat:
  - drag kiri: rotate object
  - drag kanan atau tengah: geser object
  - wheel mouse: zoom
  - panah kiri/kanan atau `,` `.`: ganti konten preview
  - `R`: reset posisi, rotasi, dan scale preview
- `UIOverlayController` juga diperbarui agar backdrop gelap otomatis disembunyikan saat webcam editor aktif.
- Efek yang dituju:
  - test visual AR lebih cepat tanpa install APK ulang
  - object bisa dicek langsung di depan webcam laptop
  - gesture desktop terasa lebih natural untuk tuning pivot, scale, dan framing

## Update Terbaru: Force Linux Editor Camera ke `/dev/video0`
- Hasil diagnosis host Linux:
  - Unity Editor membuka `/dev/video1`
  - `/dev/video1` ternyata hanya `Metadata Capture`
  - stream video kamera yang benar ada di `/dev/video0`
- Ditambahkan helper baru:
  - `Assets/Scripts/AR/LinuxEditorDirectCameraFeed.cs`
- Jalur webcam editor Linux sekarang tidak lagi mengandalkan `WebCamTexture`.
- Sebagai gantinya, editor memanggil `ffmpeg` langsung ke `/dev/video0`, lalu frame RGBA disuntikkan ke overlay Unity.
- Efek yang dituju:
  - tidak lagi salah pilih node metadata webcam
  - feed editor lebih konsisten untuk test visual langsung di Linux
