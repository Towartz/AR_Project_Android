# UJIKOM Guide

Panduan ini merangkum tempat untuk:
- ganti desain UI tiap scene
- ganti model object 3D
- ganti marker image target
- ganti barcode atau marker card
- menambah konten AR baru
- menjaga relasi asset supaya scan tetap cocok

Project ini memakai pola data-driven. Jadi perubahan visual biasanya menyentuh tiga area:
- scene UI di `Assets/Scenes`
- asset data di `Assets/ScriptableObjects` dan `Assets/Resources/ARtiGrafContentLibrary.asset`
- model atau marker di `Assets/Imported`, `Assets/Prefabs`, dan `Assets/Art`

## Peta Folder Penting

### Scene UI
- `Assets/Scenes/SplashScene.unity`
- `Assets/Scenes/MainMenuScene.unity`
- `Assets/Scenes/MaterialSelectScene.unity`
- `Assets/Scenes/GuideScene.unity`
- `Assets/Scenes/ARScanScene.unity`
- `Assets/Scenes/QuizScene.unity`
- `Assets/Scenes/ResultScene.unity`
- `Assets/Scenes/AboutScene.unity`

### Data Konten
- `Assets/Resources/ARtiGrafContentLibrary.asset`
- `Assets/ScriptableObjects/*.asset`

### Model 3D
- `Assets/Imported/BlenderDemo/*.fbx`
- `Assets/Prefabs/BlenderDemo/*.prefab`
- `Assets/Prefabs/ARObjects/*.prefab`

### Marker dan Barcode
- `Assets/Art/ReferenceImages/*.png`
- `Assets/Art/BarcodeTargets/*.png`
- `Assets/Art/BarcodeMarkers/*.png`

### Script Penghubung
- `Assets/Scripts/Core/SceneNavigationController.cs`
- `Assets/Scripts/Core/SplashController.cs`
- `Assets/Scripts/UI/UIOverlayController.cs`
- `Assets/Scripts/AR/MaterialContentController.cs`
- `Assets/Scripts/AR/ARImageTrackingController.cs`
- `Assets/Scripts/Quiz/QuizManager.cs`
- `Assets/Scripts/Quiz/ResultSceneController.cs`

### Tooling Maintenance Baru
- `Assets/Editor/ARtiGrafContentMaintenance.cs`
- `Assets/Editor/MaterialContentDataEditor.cs`
- `Assets/Editor/ARtiGrafContentDashboardWindow.cs`
- `Assets/Editor/ARtiGrafCreateContentWizardWindow.cs`
- `Assets/Editor/ARtiGrafAndroidBuildTools.cs`
- `Assets/Editor/ARtiGrafAndroidBuildWindow.cs`

Fungsi tooling baru:
- sync `ARtiGrafContentLibrary` otomatis dari `Assets/ScriptableObjects`
- validasi konten AR sebelum build
- auto-config content asset baru dari naming convention
- helper Inspector langsung pada `MaterialContentData`
- dashboard konten AR terpusat untuk maintenance harian
- wizard pembuatan content baru dari form editor
- tombol build, install, dan launch Android langsung dari editor

Menu baru di Unity:
- `Tools/ARtiGraf/Content/Create New Content`
- `Tools/ARtiGraf/Content/Open Dashboard`
- `Tools/ARtiGraf/Build/Open Android Build Window`
- `Tools/ARtiGraf/Content/Sync Library From ScriptableObjects`
- `Tools/ARtiGraf/Content/Validate Content Setup`
- `Tools/ARtiGraf/Content/Sync Library And Validate`
- `Assets/ARtiGraf/Content/Create New Content From Selection`
- `Assets/ARtiGraf/Content/Auto Configure Selected Content`

### Cara Pakai Dashboard
1. Buka `Tools/ARtiGraf/Content/Open Dashboard`.
2. Gunakan filter:
   - `All`
   - `Typography`
   - `Color`
   - `Barcode`
   - `Blend`
   - `Issues`
3. Gunakan search untuk cari `id`, nama asset, prefab, marker, atau barcode.
4. Tiap content punya aksi cepat:
   - `Select Asset`
   - `Auto Configure`
   - `Ping Prefab`
   - `Ping Marker`
   - `Ping Barcode`
5. Dari toolbar dashboard kamu juga bisa:
   - `Refresh`
   - `New Content`
   - `Android Build`
   - `Sync Library`
   - `Validate`
   - `Sync + Validate`
   - `Reveal Report`

Dashboard ini paling berguna saat:
- tambah konten baru
- rename marker atau barcode
- cek content yang belum masuk library
- cek content yang duplikat scan key
- cari mismatch antara asset data dan file fisik

### Cara Pakai Wizard Create New Content
1. Buka `Tools/ARtiGraf/Content/Create New Content`.
2. Isi field utama:
   - `Content Id`
   - `Title`
   - `Subtitle`
   - `Description`
   - `Object Type`
   - `Category`
3. Hubungkan asset bila sudah ada:
   - `Prefab`
   - `Reference Image Texture`
   - `Reference Image Name`
4. Kalau mulai dari asset yang sedang dipilih di Project, gunakan:
   - `Prefill From Selection`
   - atau menu `Assets/ARtiGraf/Content/Create New Content From Selection`
5. Kalau naming asset sudah rapi, gunakan `Auto Link By ID`.
6. Centang opsi yang dibutuhkan:
   - `Auto Add To Library`
   - `Validate After Create`
   - `Select Created Asset`
7. Lanjutkan create atau update asset content.

Wizard ini cocok saat:
- tambah object AR baru tanpa setup manual satu per satu
- bikin entry baru dari prefab Blender yang baru diimport
- bikin content dari marker atau texture yang sudah ada
- mengurangi risiko lupa sync library setelah tambah asset

### Cara Pakai Android Build Window
1. Buka `Tools/ARtiGraf/Build/Open Android Build Window`.
2. Lihat panel status:
   - `Package`
   - `Product`
   - `ADB`
   - `APK`
   - `Device`
3. Gunakan tombol utama sesuai kebutuhan:
   - `Build APK`
   - `Build + Install`
   - `Build + Install + Launch`
4. Gunakan tombol tambahan:
   - `Install Last APK`
   - `Launch Installed App`
   - `Reveal APK`
   - `Refresh`
   - `Clear Log`
5. Kalau mau pindah ke maintenance content, klik `Open Dashboard`.

Window ini cocok saat:
- mau test cepat ke HP tanpa terminal
- mau install ulang APK terakhir tanpa rebuild
- mau launch ulang app tanpa install
- mau cek apakah `adb` dan device sudah kebaca sebelum build

## Konsep Penting Sebelum Edit

### Bedakan 3 jenis target
- `ReferenceImages`: gambar acuan image target Vuforia. Ini dipakai untuk scan gambar biasa.
- `BarcodeTargets`: QR atau barcode yang payload-nya dibaca scanner, lalu dicocokkan ke `id` atau `referenceImageName`.
- `BarcodeMarkers`: gambar marker card yang bisa kamu print atau tampilkan, biasanya dipakai sebagai visual target demo Blender.

### Jalur relasi konten AR
Satu konten AR biasanya tersusun seperti ini:
1. model atau prefab
2. asset `MaterialContentData`
3. entri di `ARtiGrafContentLibrary.asset`
4. marker image atau barcode
5. scene `ARScanScene`

Kalau salah satu tidak sinkron, hasil scan bisa mismatch atau tidak muncul.

## Cara Edit Desain UI Tiap Scene

### Workflow Umum
1. Buka scene dari `Assets/Scenes`.
2. Cari `Canvas` atau `Canvas_Overlay` di `Hierarchy`.
3. Edit `RectTransform`, `Image`, `Text`, `Button`, warna, font, sprite, dan layout dari Inspector.
4. Cek object controller di scene untuk memastikan field `SerializeField` tidak putus.
5. Test di `Game View` portrait Android.

### Yang Aman Diubah
- warna
- background
- icon
- font
- ukuran teks
- ukuran tombol
- spacing
- panel
- anchor
- alignment
- padding
- sprite

### Yang Harus Hati-Hati
- menghapus object yang masih di-link ke script
- rename object penting lalu lupa update referensi di Inspector
- mengubah `On Click()` button tanpa cek scene tujuan
- memindah object keluar dari `Canvas`

## Scene-by-Scene UI Map

### 1. Splash Scene
File:
- `Assets/Scenes/SplashScene.unity`

Object utama:
- `Canvas`
- `Background`
- `TopGlow`
- `Title`
- `Tagline`
- `LoadingHint`

Script:
- `Assets/Scripts/Core/SplashController.cs`

Catatan:
- durasi splash di field `delaySeconds`
- tujuan scene berikutnya di field `nextSceneName`

### 2. Main Menu Scene
File:
- `Assets/Scenes/MainMenuScene.unity`

Object utama:
- `Canvas`
- `Background`
- `MenuPanel`
- `Title`
- `Subtitle`
- `MulaiBelajarButton`
- `PetunjukButton`
- `QuizButton`
- `TentangButton`
- `KeluarButton`

Script:
- `Assets/Scripts/Core/SceneNavigationController.cs`

### 3. Material Select Scene
File:
- `Assets/Scenes/MaterialSelectScene.unity`

Object utama:
- `Canvas`
- `Background`
- `HeroPanel`
- `HeroText`
- `Cards`
- `TipografiCard`
- `WarnaCard`
- `SelectButton`
- `BackButton`
- `BadgePanel`
- `VisualPanel`
- `Title`
- `Body`
- `EyebrowPanel`

Script:
- `Assets/Scripts/Core/SceneNavigationController.cs`

### 4. Guide Scene
File:
- `Assets/Scenes/GuideScene.unity`

Object utama:
- `Canvas`
- `Background`
- `GuidePanel`
- `Body`
- `BackButton`

Script:
- `Assets/Scripts/Core/SceneNavigationController.cs`

### 5. AR Scan Scene
File:
- `Assets/Scenes/ARScanScene.unity`

Canvas utama:
- `Canvas_Overlay`

Object utama:
- `TopBar`
- `BackButton`
- `HelpButton`
- `InfoToggleButton`
- `InfoPanel`
- `ScanGuide`
- `ScanGuideFrame`
- `HelpPanel`
- `CloseHelpButton`
- `StatusPanel`
- `TitleText`
- `SubtitleText`
- `DescriptionText`
- `CategoryText`
- `ObjectTypeText`
- `FocusText`
- `ContextText`
- `StatusText`
- `HelpText`

Script:
- `Assets/Scripts/UI/UIOverlayController.cs`
- `Assets/Scripts/AR/MaterialContentController.cs`
- `Assets/Scripts/AR/ARImageTrackingController.cs`

Catatan:
- panel ini diisi dinamis saat marker terbaca
- isi teks bukan cuma dari scene, tapi juga dari asset `MaterialContentData`

### 6. Quiz Scene
File:
- `Assets/Scenes/QuizScene.unity`

Object utama:
- `Canvas`
- `Background`
- `QuizPanel`
- `Title`
- `QuestionText`
- `ProgressText`
- `FeedbackText`
- `Option1`
- `Option2`
- `Option3`
- `BackButton`

Script:
- `Assets/Scripts/Quiz/QuizManager.cs`

Data soal:
- `Assets/ScriptableObjects/ARtiGrafQuizBank.asset`

### 7. Result Scene
File:
- `Assets/Scenes/ResultScene.unity`

Object utama:
- `Canvas`
- `Background`
- `ResultPanel`
- `Title`
- `ScoreText`
- `SummaryText`
- `FeedbackText`
- `NextStepText`
- `ReviewButton`
- `RetryButton`
- `MenuButton`

Script:
- `Assets/Scripts/Quiz/ResultSceneController.cs`
- `Assets/Scripts/Core/SceneNavigationController.cs`

### 8. About Scene
File:
- `Assets/Scenes/AboutScene.unity`

Object utama:
- `Canvas`
- `Background`
- `AboutPanel`
- `Title`
- `Body`
- `BackButton`

Script:
- `Assets/Scripts/Core/SceneNavigationController.cs`

## Cara Ganti Model Object 3D

### Kalau model baru berasal dari Blender
1. Export dari Blender ke `.fbx`.
2. Simpan atau copy ke folder seperti:
   - `Assets/Imported/BlenderDemo/`
3. Tunggu Unity import asset.
4. Drag model ke scene kosong atau ke prefab stage.
5. Rapikan scale, rotation, dan child hierarchy.
6. Simpan jadi prefab baru di:
   - `Assets/Prefabs/BlenderDemo/`
   - atau `Assets/Prefabs/ARObjects/`

### Kalau hanya ingin ganti object yang muncul dari marker lama
1. Buka asset `MaterialContentData` yang terkait di `Assets/ScriptableObjects/`.
2. Ganti field `Prefab` ke prefab baru.
3. Simpan.
4. Pastikan asset itu tetap masuk di `Assets/Resources/ARtiGrafContentLibrary.asset`.

### Field penting di asset konten
Di setiap `MaterialContentData`, field yang penting:
- `id`
- `title`
- `subtitle`
- `description`
- `referenceImageTexture`
- `prefab`
- `referenceImageName`
- `targetWidthMeters`
- `objectType`
- `colorFocus`
- `fontTypeFocus`

### Contoh jalur model Blender demo saat ini
- `Assets/Imported/BlenderDemo/blend_apple.fbx`
- `Assets/Imported/BlenderDemo/blend_cube.fbx`
- `Assets/Prefabs/BlenderDemo/blend_apple.prefab`
- `Assets/Prefabs/BlenderDemo/blend_cube.prefab`
- `Assets/ScriptableObjects/blend_apple.asset`
- `Assets/ScriptableObjects/blend_cube.asset`

### Kalau posisi atau rotasi model terasa aneh
Tempat utama yang harus kamu edit:
- prefab di `Assets/Prefabs/BlenderDemo/`
- child model di dalam prefab
- orientasi model saat import `.fbx`

Catatan:
- runtime sekarang sudah punya normalisasi pivot, tapi hasil terbaik tetap dari prefab yang rapi
- kalau model miring dari sumber, perbaiki di prefab atau Blender, bukan di scene scan

## Cara Menambah Konten AR Baru

Contoh: kamu mau tambah model `blend_robot`.

1. Import model ke `Assets/Imported/BlenderDemo/blend_robot.fbx`.
2. Buat prefab baru di `Assets/Prefabs/BlenderDemo/blend_robot.prefab`.
3. Buat asset `MaterialContentData` baru di `Assets/ScriptableObjects/blend_robot.asset`.
4. Isi field penting:
   - `id = blend_robot`
   - `title`
   - `subtitle`
   - `description`
   - `prefab = blend_robot.prefab`
   - `referenceImageTexture = marker yang dipakai`
   - `referenceImageName = blend_robot`
   - `targetWidthMeters = ukuran marker nyata`
5. Tambahkan asset itu ke list `items` pada `Assets/Resources/ARtiGrafContentLibrary.asset`.
6. Siapkan image target atau barcode target-nya.
7. Test scan di build Android.

Kalau langkah 5 dilewatkan, scanner tidak akan mengenali konten baru walaupun prefab dan marker sudah ada.

## Cara Ganti atau Menambah Image Target

### Folder yang dipakai
- `Assets/Art/ReferenceImages/`

### Kapan pakai ReferenceImages
Pakai ini kalau kamu ingin scan gambar poster, kartu materi, atau gambar marker non-QR sebagai target utama Vuforia.

### Langkah ganti image target untuk konten lama
1. Siapkan gambar PNG baru.
2. Simpan ke `Assets/Art/ReferenceImages/`.
3. Buka asset `MaterialContentData` yang terkait.
4. Ganti `ReferenceImageTexture` ke gambar baru.
5. Pastikan `ReferenceImageName` tetap sinkron dengan nama target yang kamu inginkan.
6. Sesuaikan `targetWidthMeters` dengan ukuran fisik marker cetak.
7. Build dan test ulang.

### Langkah menambah image target baru
1. Import gambar ke `Assets/Art/ReferenceImages/`.
2. Buat asset konten baru di `Assets/ScriptableObjects/`.
3. Isi `referenceImageTexture`.
4. Isi `referenceImageName`.
5. Isi `prefab`.
6. Masukkan ke `ARtiGrafContentLibrary.asset`.

### Tips marker image target yang baik
- kontras tinggi
- banyak detail unik
- tidak blur
- tidak terlalu simetris
- tidak full area kosong
- tidak terlalu gelap

## Cara Ganti Barcode atau QR Target

### Folder barcode saat ini
- `Assets/Art/BarcodeTargets/`

Contoh yang ada:
- `blend_apple.png`
- `blend_cube.png`
- `blend_cylinder.png`
- `blend_pyramid.png`
- `blend_roundcube.png`
- `blend_torus.png`
- `cubes.png`

### Cara kerja barcode di project ini
Scanner barcode akan mencoba mencocokkan payload ke:
- `id`
- atau `referenceImageName`

Payload dinormalisasi. Jadi:
- `blend_apple`
- `blend-apple`
- `blend apple`

akan dianggap serupa setelah normalisasi.

### Barcode-only content
Konten barcode murni seperti debug cube bisa punya:
- `referenceImageTexture = kosong`
- `id = cubes`

Contohnya:
- `Assets/ScriptableObjects/barcode_cubes.asset`

### Langkah ganti barcode
1. Buat QR atau barcode baru dengan payload yang kamu inginkan.
2. Simpan ke `Assets/Art/BarcodeTargets/`.
3. Pastikan payload itu cocok dengan `id` atau `referenceImageName` pada asset konten.
4. Jika perlu, edit `MaterialContentData` yang terkait.
5. Pastikan asset konten ada di `ARtiGrafContentLibrary.asset`.
6. Build dan test di HP.

### Contoh payload yang aman
- `blend_apple`
- `blend_cube`
- `blend_pyramid`
- `cubes`

### Kesalahan yang sering bikin barcode tidak muncul
- payload QR tidak sama dengan `id`
- asset belum dimasukkan ke `ARtiGrafContentLibrary.asset`
- `Prefab` kosong
- scene build masih versi lama

## Cara Ganti Marker Card yang Dicetak

### Folder marker card
- `Assets/Art/BarcodeMarkers/`

Contoh:
- `blend_apple_marker.png`
- `blend_cube_marker.png`
- `blend_cylinder_marker.png`
- `blend_pyramid_marker.png`
- `blend_roundcube_marker.png`
- `blend_torus_marker.png`

Marker ini berguna sebagai kartu scan atau visual print-ready. Kalau kamu ingin mengganti desain kartu marker:
1. edit atau buat PNG baru
2. simpan ke `Assets/Art/BarcodeMarkers/`
3. jaga nama file tetap konsisten dengan konten yang dipakai
4. kalau marker baru juga jadi image target, hubungkan juga ke `referenceImageTexture`

## Perbedaan Praktis: ReferenceImages vs BarcodeTargets vs BarcodeMarkers

### ReferenceImages
- dipakai Vuforia image target
- cocok untuk scan poster atau kartu gambar
- harus terhubung ke `referenceImageTexture`

### BarcodeTargets
- dipakai scanner barcode
- isi utamanya payload
- payload harus cocok dengan `id` atau `referenceImageName`

### BarcodeMarkers
- kartu desain siap print atau siap tampil di layar
- bisa dipakai sebagai materi bantu scan
- biasanya bukan sumber data utama, tetapi visual marker card

## Cara Mengganti Konten Info yang Muncul di Panel AR

Buka asset terkait di `Assets/ScriptableObjects/` lalu ubah:
- `title`
- `subtitle`
- `description`
- `objectType`
- `colorFocus`
- `fontTypeFocus`

Perubahan ini akan muncul di panel info `ARScanScene` melalui:
- `Assets/Scripts/UI/UIOverlayController.cs`
- `Assets/Scripts/AR/MaterialContentController.cs`

## Cara Mengganti Soal Quiz

File:
- `Assets/ScriptableObjects/ARtiGrafQuizBank.asset`

Di sana kamu bisa ganti:
- pertanyaan
- opsi jawaban
- indeks jawaban benar
- penjelasan

UI quiz tetap di scene:
- `Assets/Scenes/QuizScene.unity`

## Tempat Mengubah Navigasi Tombol

Script utama:
- `Assets/Scripts/Core/SceneNavigationController.cs`

Di Unity Editor:
1. pilih tombol di scene
2. cek komponen `Button`
3. lihat bagian `On Click()`
4. pastikan method yang dipanggil tetap benar

## Checklist Saat Mengganti Model atau Marker

Sebelum build, cek semua ini:
- prefab sudah benar
- asset `MaterialContentData` sudah benar
- `id` sudah benar
- `referenceImageTexture` sudah benar kalau pakai image target
- `referenceImageName` sudah sinkron
- payload barcode sudah sinkron
- item sudah masuk ke `ARtiGrafContentLibrary.asset`
- tidak ada field Inspector yang putus di scene

## Troubleshooting Cepat

### Scan marker berhasil tapi object salah
Penyebab umum:
- payload barcode cocok ke asset yang salah
- `id` dan `referenceImageName` bentrok
- asset library belum rapi

Solusi:
- cek `Assets/ScriptableObjects/*.asset`
- cek `Assets/Resources/ARtiGrafContentLibrary.asset`

### Scan marker tidak memunculkan apa-apa
Penyebab umum:
- prefab kosong
- asset belum masuk library
- barcode payload tidak cocok
- `referenceImageTexture` kosong untuk image target

### Object muncul tapi posisi aneh
Penyebab umum:
- pivot prefab jelek
- rotasi model import jelek
- child model di prefab offset

Solusi:
- rapikan prefab
- cek transform child model
- bila perlu perbaiki dari Blender

### UI AR berubah tapi saat scan teks tetap lain
Penyebab umum:
- teks diisi runtime dari asset `MaterialContentData`

Solusi:
- edit asset data, bukan hanya teks scene

## Rekomendasi Workflow Yang Aman
1. Duplikat asset atau scene sebelum redesign besar.
2. Ganti satu hal dulu.
3. Test di Editor.
4. Build ke Android.
5. Test scan marker nyata di HP.
6. Baru lanjut perubahan berikutnya.

## Kalau Mau Bikin Project Ini Lebih Enak Dikelola
- buat prefab tombol global
- buat prefab card global
- buat prefab panel global
- bikin naming rule untuk `id`, marker, prefab, dan barcode
- migrasi `Text` lama ke `TextMeshPro`
- bikin style guide warna dan font
- pisahkan konten demo dan konten final

Dengan struktur itu, nanti kamu bisa ganti desain scene, ganti object 3D, dan ganti marker jauh lebih cepat tanpa takut ada relasi asset yang putus.

## Panduan Praktis Per Kasus

Bagian ini dibuat untuk workflow cepat. Tinggal pilih kasus yang mau kamu ubah, lalu ikuti urutannya.

### Kasus 1: Ganti desain satu scene
Contoh:
- redesign `MainMenuScene`
- redesign `MaterialSelectScene`
- redesign `ARScanScene`

Langkah:
1. Buka scene target dari `Assets/Scenes`.
2. Cari `Canvas` atau `Canvas_Overlay`.
3. Edit:
   - background
   - panel
   - button
   - font
   - layout
   - spacing
   - size
4. Klik object controller di scene, lalu cek semua field `SerializeField` masih terhubung.
5. Test di `Game View`.
6. Kalau scene itu penting untuk mobile, build ulang dan test di HP.

Checklist:
- `On Click()` button masih benar
- text yang dinamis tidak tertimpa manual
- object UI tidak keluar dari `Canvas`
- layout masih enak di portrait

### Kasus 2: Ganti object yang muncul dari marker lama
Contoh:
- marker `blend_apple` sekarang mau munculkan model lain

Langkah:
1. Siapkan prefab object baru.
2. Buka asset konten lama di `Assets/ScriptableObjects/`.
3. Ganti field `Prefab`.
4. Simpan.
5. Test scan marker yang sama.

Yang berubah:
- object 3D

Yang tetap:
- `id`
- marker
- barcode payload
- info scan lain, kalau tidak kamu ubah manual

Checklist:
- prefab baru tidak kosong
- prefab scale normal
- prefab tidak punya child offset aneh
- collider atau komponen tidak merusak performa

### Kasus 3: Ganti model Blender lama dengan export Blender terbaru
Contoh:
- kamu update `Apple_Model.blend`
- mau ganti isi `blend_apple`

Langkah:
1. Export model baru dari Blender ke `.fbx`.
2. Replace atau import ke `Assets/Imported/BlenderDemo/`.
3. Buka prefab lama di `Assets/Prefabs/BlenderDemo/`.
4. Ganti child model lama dengan model import baru.
5. Rapikan:
   - position
   - rotation
   - scale
   - pivot hierarchy
6. Simpan prefab.
7. Test di Editor dan Android.

Checklist:
- orientasi depan model benar
- model tidak miring
- ukuran tidak terlalu besar
- material masih masuk
- object tidak terlalu berat untuk HP

### Kasus 4: Tambah object AR baru dari nol
Contoh:
- tambah `blend_robot`

Langkah:
1. Import model ke `Assets/Imported/BlenderDemo/`.
2. Buat prefab baru di `Assets/Prefabs/BlenderDemo/`.
3. Buat asset baru `MaterialContentData` di `Assets/ScriptableObjects/`.
4. Isi field:
   - `id`
   - `title`
   - `subtitle`
   - `description`
   - `prefab`
   - `referenceImageTexture`
   - `referenceImageName`
   - `targetWidthMeters`
   - `objectType`
5. Tambahkan asset itu ke `Assets/Resources/ARtiGrafContentLibrary.asset`.
6. Siapkan marker image atau barcode-nya.
7. Build dan test.

Checklist:
- asset baru masuk library
- `id` unik
- `referenceImageName` tidak bentrok
- prefab benar
- marker benar

### Kasus 5: Ganti image target untuk konten tertentu
Contoh:
- `blend_apple` sebelumnya pakai marker lama, sekarang mau pakai gambar baru

Langkah:
1. Import PNG baru ke `Assets/Art/ReferenceImages/`.
2. Buka asset konten target di `Assets/ScriptableObjects/`.
3. Ganti `ReferenceImageTexture`.
4. Atur `ReferenceImageName` bila perlu.
5. Sesuaikan `targetWidthMeters`.
6. Build ulang.
7. Test scan image target baru.

Checklist:
- gambar tidak blur
- kontras cukup
- banyak detail unik
- ukuran fisik marker sesuai `targetWidthMeters`

### Kasus 6: Ganti barcode atau QR payload
Contoh:
- QR lama `blend_cube`, sekarang mau jadi `blend_robot`

Langkah:
1. Tentukan payload baru.
2. Pastikan payload itu sama dengan:
   - `id`
   - atau `referenceImageName`
3. Generate QR baru.
4. Simpan hasilnya ke `Assets/Art/BarcodeTargets/`.
5. Kalau perlu, edit asset `MaterialContentData` yang terkait.
6. Build dan test scan barcode.

Checklist:
- payload persis cocok
- asset konten ada di library
- prefab tidak kosong
- HP fokus saat scan

### Kasus 7: Ganti desain marker card yang mau diprint
Contoh:
- `blend_apple_marker.png` mau diganti desainnya

Langkah:
1. Edit atau buat PNG marker baru.
2. Simpan ke `Assets/Art/BarcodeMarkers/`.
3. Jika marker card itu juga jadi image target, sinkronkan dengan `ReferenceImageTexture`.
4. Print atau tampilkan marker baru untuk test.

Checklist:
- marker cukup terang
- tidak blur
- tidak kena crop
- nama file tetap konsisten

### Kasus 8: Ganti isi panel info AR
Contoh:
- ubah judul
- ubah deskripsi
- ubah kategori fokus

Langkah:
1. Buka asset konten terkait di `Assets/ScriptableObjects/`.
2. Ubah:
   - `title`
   - `subtitle`
   - `description`
   - `objectType`
   - `colorFocus`
   - `fontTypeFocus`
3. Simpan.
4. Test scan object itu.

Checklist:
- isi teks sesuai object
- kategori tidak salah
- deskripsi tidak kepanjangan untuk panel HP

### Kasus 9: Ganti soal quiz
Langkah:
1. Buka `Assets/ScriptableObjects/ARtiGrafQuizBank.asset`.
2. Edit pertanyaan.
3. Edit opsi.
4. Edit indeks jawaban benar.
5. Edit penjelasan.
6. Test di `QuizScene`.

Checklist:
- jumlah opsi konsisten
- jawaban benar tidak salah index
- feedback tetap masuk akal

### Kasus 10: Ganti semua identitas satu konten secara total
Contoh:
- `blend_apple` mau diubah jadi `blend_orange`

Yang perlu diganti:
1. model `.fbx`
2. prefab
3. asset `MaterialContentData`
4. `id`
5. `title`
6. `subtitle`
7. `description`
8. `referenceImageTexture`
9. `referenceImageName`
10. barcode target
11. marker card
12. entri di `ARtiGrafContentLibrary.asset`

Urutan aman:
1. siapkan model
2. siapkan prefab
3. duplikat asset konten lama
4. ubah data ke identitas baru
5. siapkan reference image
6. siapkan barcode
7. siapkan marker card
8. masukkan ke library
9. test
10. hapus versi lama kalau memang sudah tidak dipakai

## Matrix Cepat: Kalau Mau Ubah X, Edit File Mana

### Ubah tampilan tombol dan layout scene
- `Assets/Scenes/*.unity`

### Ubah scene tujuan tombol
- `Assets/Scripts/Core/SceneNavigationController.cs`
- `Button -> On Click()` di scene

### Ubah teks info AR yang muncul saat scan
- `Assets/ScriptableObjects/*.asset`

### Ubah object 3D yang muncul saat scan
- `Assets/ScriptableObjects/*.asset`
- field `Prefab`

### Ubah model dasar dari Blender
- `Assets/Imported/BlenderDemo/*.fbx`
- `Assets/Prefabs/BlenderDemo/*.prefab`

### Ubah marker image target
- `Assets/Art/ReferenceImages/*.png`
- `Assets/ScriptableObjects/*.asset`

### Ubah QR atau barcode
- `Assets/Art/BarcodeTargets/*.png`
- `Assets/ScriptableObjects/*.asset`

### Ubah marker card print-ready
- `Assets/Art/BarcodeMarkers/*.png`

### Ubah soal quiz
- `Assets/ScriptableObjects/ARtiGrafQuizBank.asset`

## Naming Rule Yang Disarankan

Supaya tidak mismatch, pakai pola ini:
- model import: `blend_namaobject.fbx`
- prefab: `blend_namaobject.prefab`
- asset data: `blend_namaobject.asset`
- reference image name: `blend_namaobject`
- barcode payload: `blend_namaobject`
- marker card: `blend_namaobject_marker.png`

Contoh:
- `blend_apple.fbx`
- `blend_apple.prefab`
- `blend_apple.asset`
- `referenceImageName = blend_apple`
- barcode payload = `blend_apple`
- marker card = `blend_apple_marker.png`

Kalau naming konsisten, kemungkinan mismatch akan jauh turun.

## Workflow Aman Sebelum Build Android

1. Save scene dan prefab.
2. Kalau ada content baru, buat dulu lewat `Tools/ARtiGraf/Content/Create New Content` atau tombol `New Content` di dashboard.
3. Bila perlu, jalankan `Auto Link By ID` atau `Auto Configure`.
4. Buka `Tools/ARtiGraf/Content/Open Dashboard`.
5. Jalankan `Sync + Validate` dari dashboard atau menu `Tools`.
6. Cek `UJIKOM_Content_Report.md` bila validator menandai issue.
7. Cek `ARtiGrafContentLibrary.asset`.
8. Cek field `Prefab`, `ReferenceImageTexture`, `ReferenceImageName`, dan `id`.
9. Buka `ARScanScene`.
10. Pastikan controller tidak kehilangan referensi.
11. Buka `Tools/ARtiGraf/Build/Open Android Build Window`.
12. Pilih salah satu:
   - `Build APK`
   - `Build + Install`
   - `Build + Install + Launch`
13. Kalau perlu install ulang cepat, gunakan `Install Last APK`.
14. Test:
   - scan image target
   - scan barcode
   - ganti marker beberapa kali
   - rotate, scale, dan pan object

## Checklist Final Saat Ada Bug Mismatch

Kalau marker A memunculkan object B, cek:
- payload barcode marker A
- `id` asset A
- `referenceImageName` asset A
- apakah ada asset lain dengan nama terlalu mirip
- apakah build di HP benar-benar versi terbaru

Kalau object tidak muncul sama sekali, cek:
- asset masuk library
- prefab terisi
- reference image terisi bila pakai image target
- barcode payload cocok
- marker jelas dan fokus

Kalau object muncul tapi salah posisi, cek:
- prefab root
- child model transform
- import rotation model
- scale prefab
