# UJIKOM Asset Map

Dokumen ini adalah peta asset teknis untuk project UJIKOM AR.

Tujuannya:
- menunjukkan hubungan `asset data -> prefab -> model -> marker -> barcode`
- mempermudah ganti object, marker, barcode, atau konten scan
- mengurangi bug mismatch antara marker dan object

## Ringkasan

Library konten utama:
- `Assets/Resources/ARtiGrafContentLibrary.asset`

Script penghubung utama:
- `Assets/Scripts/Data/MaterialContentData.cs`
- `Assets/Scripts/Data/MaterialContentLibrary.cs`
- `Assets/Scripts/AR/MaterialContentController.cs`
- `Assets/Scripts/AR/ARImageTrackingController.cs`

Kategori:
- `0 = Typography`
- `1 = Color`

Jumlah konten aktif saat ini:
- `23` item di `ARtiGrafContentLibrary.asset`

Pembagian:
- `8` konten tipografi standar
- `8` konten warna standar
- `6` konten demo Blender hybrid
- `1` konten debug barcode-only

## Aturan Resolusi Scan Saat Ini

Barcode dan marker scan dicocokkan terutama ke:
- `id`
- `referenceImageName`

Catatan penting:
- `barcode_cubes.asset` adalah konten barcode-only
- alias payload `cube` dan `debugcube` dipetakan ke `cubes`
- konten `blend_*` saat ini adalah konten hybrid:
  - punya image target dari `Assets/Art/BarcodeMarkers/*_marker.png`
  - punya barcode target dari `Assets/Art/BarcodeTargets/*.png`

## Root Folder Penting

Data asset:
- `Assets/ScriptableObjects/`

Library:
- `Assets/Resources/ARtiGrafContentLibrary.asset`

Prefab Blender demo:
- `Assets/Prefabs/BlenderDemo/`

Source model Blender:
- `Assets/Imported/BlenderDemo/`

Prefab object standar:
- `Assets/Prefabs/ARObjects/`

Image target standar:
- `Assets/Art/ReferenceImages/`

Barcode target:
- `Assets/Art/BarcodeTargets/`

Marker card / marker image:
- `Assets/Art/BarcodeMarkers/`

## A. Konten Hybrid Blender Demo

Konten di bagian ini bisa di-scan lewat:
- marker image target
- barcode atau QR payload

### 1. blend_apple
- Category: `Typography`
- Data asset: `Assets/ScriptableObjects/blend_apple.asset`
- Library: `Assets/Resources/ARtiGrafContentLibrary.asset`
- Prefab: `Assets/Prefabs/BlenderDemo/blend_apple.prefab`
- Source model: `Assets/Imported/BlenderDemo/blend_apple.fbx`
- Reference image texture: `Assets/Art/BarcodeMarkers/blend_apple_marker.png`
- Barcode target: `Assets/Art/BarcodeTargets/blend_apple.png`
- Marker card: `Assets/Art/BarcodeMarkers/blend_apple_marker.png`
- Scan key `id`: `blend_apple`
- Scan key `referenceImageName`: `blend_apple`
- Target width meters: `0.12`
- Object type: `Apple`

### 2. blend_cube
- Category: `Typography`
- Data asset: `Assets/ScriptableObjects/blend_cube.asset`
- Library: `Assets/Resources/ARtiGrafContentLibrary.asset`
- Prefab: `Assets/Prefabs/BlenderDemo/blend_cube.prefab`
- Source model: `Assets/Imported/BlenderDemo/blend_cube.fbx`
- Reference image texture: `Assets/Art/BarcodeMarkers/blend_cube_marker.png`
- Barcode target: `Assets/Art/BarcodeTargets/blend_cube.png`
- Marker card: `Assets/Art/BarcodeMarkers/blend_cube_marker.png`
- Scan key `id`: `blend_cube`
- Scan key `referenceImageName`: `blend_cube`
- Target width meters: `0.12`
- Object type: `Cube`

### 3. blend_roundcube
- Category: `Typography`
- Data asset: `Assets/ScriptableObjects/blend_roundcube.asset`
- Library: `Assets/Resources/ARtiGrafContentLibrary.asset`
- Prefab: `Assets/Prefabs/BlenderDemo/blend_roundcube.prefab`
- Source model: `Assets/Imported/BlenderDemo/blend_roundcube.fbx`
- Reference image texture: `Assets/Art/BarcodeMarkers/blend_roundcube_marker.png`
- Barcode target: `Assets/Art/BarcodeTargets/blend_roundcube.png`
- Marker card: `Assets/Art/BarcodeMarkers/blend_roundcube_marker.png`
- Scan key `id`: `blend_roundcube`
- Scan key `referenceImageName`: `blend_roundcube`
- Target width meters: `0.12`
- Object type: `Round Cube`

### 4. blend_cylinder
- Category: `Color`
- Data asset: `Assets/ScriptableObjects/blend_cylinder.asset`
- Library: `Assets/Resources/ARtiGrafContentLibrary.asset`
- Prefab: `Assets/Prefabs/BlenderDemo/blend_cylinder.prefab`
- Source model: `Assets/Imported/BlenderDemo/blend_cylinder.fbx`
- Reference image texture: `Assets/Art/BarcodeMarkers/blend_cylinder_marker.png`
- Barcode target: `Assets/Art/BarcodeTargets/blend_cylinder.png`
- Marker card: `Assets/Art/BarcodeMarkers/blend_cylinder_marker.png`
- Scan key `id`: `blend_cylinder`
- Scan key `referenceImageName`: `blend_cylinder`
- Target width meters: `0.12`
- Object type: `Cylinder`

### 5. blend_pyramid
- Category: `Color`
- Data asset: `Assets/ScriptableObjects/blend_pyramid.asset`
- Library: `Assets/Resources/ARtiGrafContentLibrary.asset`
- Prefab: `Assets/Prefabs/BlenderDemo/blend_pyramid.prefab`
- Source model: `Assets/Imported/BlenderDemo/blend_pyramid.fbx`
- Reference image texture: `Assets/Art/BarcodeMarkers/blend_pyramid_marker.png`
- Barcode target: `Assets/Art/BarcodeTargets/blend_pyramid.png`
- Marker card: `Assets/Art/BarcodeMarkers/blend_pyramid_marker.png`
- Scan key `id`: `blend_pyramid`
- Scan key `referenceImageName`: `blend_pyramid`
- Target width meters: `0.12`
- Object type: `Pyramid`

### 6. blend_torus
- Category: `Color`
- Data asset: `Assets/ScriptableObjects/blend_torus.asset`
- Library: `Assets/Resources/ARtiGrafContentLibrary.asset`
- Prefab: `Assets/Prefabs/BlenderDemo/blend_torus.prefab`
- Source model: `Assets/Imported/BlenderDemo/blend_torus.fbx`
- Reference image texture: `Assets/Art/BarcodeMarkers/blend_torus_marker.png`
- Barcode target: `Assets/Art/BarcodeTargets/blend_torus.png`
- Marker card: `Assets/Art/BarcodeMarkers/blend_torus_marker.png`
- Scan key `id`: `blend_torus`
- Scan key `referenceImageName`: `blend_torus`
- Target width meters: `0.12`
- Object type: `Torus`

## B. Konten Debug Barcode-Only

### 1. cubes
- Category: `Typography`
- Data asset: `Assets/ScriptableObjects/barcode_cubes.asset`
- Library: `Assets/Resources/ARtiGrafContentLibrary.asset`
- Prefab: `Assets/MobileARTemplateAssets/Prefabs/CubeVariant.prefab`
- Source model: `Tidak ada source fbx khusus di map ini`
- Reference image texture: `Kosong`
- Barcode target: `Assets/Art/BarcodeTargets/cubes.png`
- Marker card: `Tidak ada marker image khusus`
- Scan key `id`: `cubes`
- Scan key `referenceImageName`: `Kosong`
- Target width meters: `0.12`
- Object type: `Cube`
- Alias barcode: `cube`, `debugcube`

## C. Konten Tipografi Standar

Konten di bagian ini saat ini memakai image target standar dari folder:
- `Assets/Art/ReferenceImages/`

Mereka tidak punya barcode target khusus di asset map saat ini.

### 1. typo_blackletter
- Data asset: `Assets/ScriptableObjects/typo_blackletter.asset`
- Prefab: `Assets/Prefabs/ARObjects/BlackletterGrapesPrefab.prefab`
- Reference image texture: `Assets/Art/ReferenceImages/typo_blackletter.png`
- Scan key `id`: `typo_blackletter`
- Scan key `referenceImageName`: `typo_blackletter`
- Object type: `Anggur klasik`

### 2. typo_display
- Data asset: `Assets/ScriptableObjects/typo_display.asset`
- Prefab: `Assets/Prefabs/ARObjects/OrangePrefab.prefab`
- Reference image texture: `Assets/Art/ReferenceImages/typo_display.png`
- Scan key `id`: `typo_display`
- Scan key `referenceImageName`: `typo_display`
- Object type: `Jeruk`

### 3. typo_geometric
- Data asset: `Assets/ScriptableObjects/typo_geometric.asset`
- Prefab: `Assets/Prefabs/ARObjects/GeometricApplePrefab.prefab`
- Reference image texture: `Assets/Art/ReferenceImages/typo_geometric.png`
- Scan key `id`: `typo_geometric`
- Scan key `referenceImageName`: `typo_geometric`
- Object type: `Apel geometrik`

### 4. typo_monospace
- Data asset: `Assets/ScriptableObjects/typo_monospace.asset`
- Prefab: `Assets/Prefabs/ARObjects/MonospaceBirdPrefab.prefab`
- Reference image texture: `Assets/Art/ReferenceImages/typo_monospace.png`
- Scan key `id`: `typo_monospace`
- Scan key `referenceImageName`: `typo_monospace`
- Object type: `Burung digital`

### 5. typo_sans
- Data asset: `Assets/ScriptableObjects/typo_sans.asset`
- Prefab: `Assets/Prefabs/ARObjects/ApplePrefab.prefab`
- Reference image texture: `Assets/Art/ReferenceImages/typo_sans.png`
- Scan key `id`: `typo_sans`
- Scan key `referenceImageName`: `typo_sans`
- Object type: `Apel`

### 6. typo_script
- Data asset: `Assets/ScriptableObjects/typo_script.asset`
- Prefab: `Assets/Prefabs/ARObjects/ButterflyPrefab.prefab`
- Reference image texture: `Assets/Art/ReferenceImages/typo_script.png`
- Scan key `id`: `typo_script`
- Scan key `referenceImageName`: `typo_script`
- Object type: `Kupu-kupu`

### 7. typo_serif
- Data asset: `Assets/ScriptableObjects/typo_serif.asset`
- Prefab: `Assets/Prefabs/ARObjects/CatPrefab.prefab`
- Reference image texture: `Assets/Art/ReferenceImages/typo_serif.png`
- Scan key `id`: `typo_serif`
- Scan key `referenceImageName`: `typo_serif`
- Object type: `Kucing`

### 8. typo_slab
- Data asset: `Assets/ScriptableObjects/typo_slab.asset`
- Prefab: `Assets/Prefabs/ARObjects/SlabBananaPrefab.prefab`
- Reference image texture: `Assets/Art/ReferenceImages/typo_slab.png`
- Scan key `id`: `typo_slab`
- Scan key `referenceImageName`: `typo_slab`
- Object type: `Pisang poster`

## D. Konten Warna Standar

Konten di bagian ini saat ini memakai image target standar dari folder:
- `Assets/Art/ReferenceImages/`

Mereka tidak punya barcode target khusus di asset map saat ini.

### 1. color_contrast
- Data asset: `Assets/ScriptableObjects/color_contrast.asset`
- Prefab: `Assets/Prefabs/ARObjects/FishPrefab.prefab`
- Reference image texture: `Assets/Art/ReferenceImages/color_contrast.png`
- Scan key `id`: `color_contrast`
- Scan key `referenceImageName`: `color_contrast`
- Object type: `Ikan badut`

### 2. color_cool
- Data asset: `Assets/ScriptableObjects/color_cool.asset`
- Prefab: `Assets/Prefabs/ARObjects/BirdPrefab.prefab`
- Reference image texture: `Assets/Art/ReferenceImages/color_cool.png`
- Scan key `id`: `color_cool`
- Scan key `referenceImageName`: `color_cool`
- Object type: `Burung`

### 3. color_earth
- Data asset: `Assets/ScriptableObjects/color_earth.asset`
- Prefab: `Assets/Prefabs/ARObjects/EarthCatPrefab.prefab`
- Reference image texture: `Assets/Art/ReferenceImages/color_earth.png`
- Scan key `id`: `color_earth`
- Scan key `referenceImageName`: `color_earth`
- Object type: `Kucing bumi`

### 4. color_harmony
- Data asset: `Assets/ScriptableObjects/color_harmony.asset`
- Prefab: `Assets/Prefabs/ARObjects/GrapesPrefab.prefab`
- Reference image texture: `Assets/Art/ReferenceImages/color_harmony.png`
- Scan key `id`: `color_harmony`
- Scan key `referenceImageName`: `color_harmony`
- Object type: `Anggur`

### 5. color_pastel
- Data asset: `Assets/ScriptableObjects/color_pastel.asset`
- Prefab: `Assets/Prefabs/ARObjects/PastelBirdPrefab.prefab`
- Reference image texture: `Assets/Art/ReferenceImages/color_pastel.png`
- Scan key `id`: `color_pastel`
- Scan key `referenceImageName`: `color_pastel`
- Object type: `Burung pastel`

### 6. color_primary
- Data asset: `Assets/ScriptableObjects/color_primary.asset`
- Prefab: `Assets/Prefabs/ARObjects/PrimaryButterflyPrefab.prefab`
- Reference image texture: `Assets/Art/ReferenceImages/color_primary.png`
- Scan key `id`: `color_primary`
- Scan key `referenceImageName`: `color_primary`
- Object type: `Kupu-kupu primer`

### 7. color_secondary
- Data asset: `Assets/ScriptableObjects/color_secondary.asset`
- Prefab: `Assets/Prefabs/ARObjects/SecondaryOrangePrefab.prefab`
- Reference image texture: `Assets/Art/ReferenceImages/color_secondary.png`
- Scan key `id`: `color_secondary`
- Scan key `referenceImageName`: `color_secondary`
- Object type: `Jeruk sekunder`

### 8. color_warm
- Data asset: `Assets/ScriptableObjects/color_warm.asset`
- Prefab: `Assets/Prefabs/ARObjects/BananaPrefab.prefab`
- Reference image texture: `Assets/Art/ReferenceImages/color_warm.png`
- Scan key `id`: `color_warm`
- Scan key `referenceImageName`: `color_warm`
- Object type: `Pisang`

## E. Cara Membaca Dependency Satu Konten

Contoh alur untuk `blend_apple`:
1. scanner membaca marker atau barcode
2. scanner cocokkan ke `id = blend_apple` atau `referenceImageName = blend_apple`
3. `MaterialContentLibrary` menemukan `Assets/ScriptableObjects/blend_apple.asset`
4. asset itu memanggil prefab `Assets/Prefabs/BlenderDemo/blend_apple.prefab`
5. prefab itu bersumber dari `Assets/Imported/BlenderDemo/blend_apple.fbx`
6. overlay UI mengisi judul, subtitle, dan deskripsi dari asset data

## F. Kalau Mau Ganti X, Edit Bagian Mana

### Ganti model object saja
Edit:
- `Assets/Prefabs/BlenderDemo/*.prefab`
- atau `Assets/Prefabs/ARObjects/*.prefab`
- atau field `Prefab` pada `Assets/ScriptableObjects/*.asset`

### Ganti source model Blender
Edit:
- `Assets/Imported/BlenderDemo/*.fbx`
- lalu rapikan prefab di `Assets/Prefabs/BlenderDemo/*.prefab`

### Ganti marker image target
Edit:
- `Assets/Art/ReferenceImages/*.png`
- atau `Assets/Art/BarcodeMarkers/*_marker.png`
- lalu update field `referenceImageTexture`

### Ganti barcode atau QR
Edit:
- `Assets/Art/BarcodeTargets/*.png`
- lalu pastikan payload cocok ke `id` atau `referenceImageName`

### Ganti isi panel informasi
Edit:
- `title`
- `subtitle`
- `description`
- `objectType`
- `colorFocus`
- `fontTypeFocus`

Lokasi edit:
- `Assets/ScriptableObjects/*.asset`

## G. Checklist Sinkronisasi

Kalau kamu mengganti satu konten, cek ini:
- `id` benar
- `referenceImageName` benar
- `Prefab` benar
- `referenceImageTexture` benar
- barcode target benar
- marker card benar
- asset sudah ada di `ARtiGrafContentLibrary.asset`

## H. Checklist Rename Aman

Kalau rename total, misalnya `blend_apple` jadi `blend_orange`, ubah semua ini:
- nama `.fbx`
- nama prefab
- nama asset data
- field `id`
- field `referenceImageName`
- nama barcode target
- nama marker card
- referensi library

Kalau salah satu tertinggal, scan bisa:
- tidak memunculkan object
- memunculkan object yang salah
- mismatch antar marker dan object

## I. Catatan Praktis

Konten yang paling sensitif terhadap mismatch saat ini:
- `blend_*`
- `barcode_cubes`

Karena konten ini menyentuh:
- marker image
- barcode payload
- prefab demo
- data asset custom

Untuk perubahan besar, workflow paling aman:
1. duplikat asset lama
2. ubah satu identitas baru
3. test di Editor
4. build ke Android
5. scan marker nyata di HP
