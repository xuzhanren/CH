window.initListingReviewPhotoEditor = (options) => {
    const MAX_PHOTOS = 4;

    const addButton = document.getElementById(options.addButtonId);
    const fileInput = document.getElementById(options.fileInputId);
    const previewList = document.getElementById(options.previewListId);
    const photosJsonInput = document.getElementById(options.photosJsonInputId);

    const cropModalElement = document.getElementById("reviewPhotoCropModal");

    if (!addButton || !fileInput || !previewList || !photosJsonInput || !cropModalElement) {
        return;
    }

    const photoTitleEditor = document.getElementById("photoTitleEditor");
    const photoSortOrderEditor = document.getElementById("photoSortOrderEditor");
    const editBrushSizeInput = document.getElementById("editBrushSizeInput");
    const editBrushSizeValue = document.getElementById("editBrushSizeValue");

    if (!photoTitleEditor || !photoSortOrderEditor || !editBrushSizeInput || !editBrushSizeValue) {
        return;
    }

    let pendingPhotos = [];

    let editingPendingIndex = null;
    let editingExistingGuid = null;

    const syncPhotosJson = () => {
        photosJsonInput.value = JSON.stringify(pendingPhotos);
    };

    const getAvailableSlots = () => {
        const existingDeleteChecks = Array.from(document.querySelectorAll("input[name='deleteImageGuids']:checked"));
        const existingItems = Array.from(document.querySelectorAll(".existing-review-photo"));
        const activeExisting = Math.max(0, existingItems.length - existingDeleteChecks.length);
        return Math.max(0, MAX_PHOTOS - activeExisting - pendingPhotos.length);
    };

    const renderPhotos = () => {
        previewList.innerHTML = "";

        pendingPhotos.forEach((photo, index) => {
            const card = document.createElement("div");
            card.className = "border rounded p-1";
            card.innerHTML = `<img src="${photo.thumbnailDataUrl || photo.fullImageDataUrl}" alt="Review photo" data-photo-index="${index}" style="width:120px;height:90px;object-fit:contain;cursor:pointer;" /><div class="small mt-1 text-truncate" style="max-width:120px;">${photo.title || "Photo"}</div><div class="text-center mt-1"><button type="button" class="btn btn-sm btn-outline-danger" data-remove-index="${index}">Remove</button></div>`;
            previewList.appendChild(card);
        });

        previewList.querySelectorAll("img[data-photo-index]").forEach((imageElement) => {
            imageElement.addEventListener("click", () => {
                const index = Number(imageElement.getAttribute("data-photo-index"));
                if (!Number.isFinite(index)) {
                    return;
                }

                const target = pendingPhotos[index];
                if (!target) {
                    return;
                }

                editingPendingIndex = index;
                editingExistingGuid = null;
                openEditor(target.fullImageDataUrl || target.thumbnailDataUrl || "", target.title || "", target.sortOrder ?? index);
            });
        });

        previewList.querySelectorAll("button[data-remove-index]").forEach((button) => {
            button.addEventListener("click", () => {
                const index = Number(button.getAttribute("data-remove-index"));
                if (!Number.isFinite(index)) {
                    return;
                }

                pendingPhotos.splice(index, 1);
                syncPhotosJson();
                renderPhotos();
            });
        });
    };

    const editor = window.CanHappyImageClipping.createEditor({
        modal: cropModalElement,
        onSave: ({ canvas }) => {
            const fullImageDataUrl = window.CanHappyImageClipping.buildFullImageDataUrl(canvas);
            const thumbnailDataUrl = window.CanHappyImageClipping.buildThumbnailDataUrl(canvas);
            const title = (photoTitleEditor.value || "").trim() || null;
            const sortOrder = Number.parseInt(photoSortOrderEditor.value || "0", 10);

            if (Number.isInteger(editingPendingIndex) && editingPendingIndex >= 0 && editingPendingIndex < pendingPhotos.length) {
                pendingPhotos[editingPendingIndex] = {
                    ...pendingPhotos[editingPendingIndex],
                    fullImageDataUrl,
                    thumbnailDataUrl,
                    title,
                    sortOrder: Number.isFinite(sortOrder) ? sortOrder : 0
                };
            } else {
                if (editingExistingGuid) {
                    const deleteCheckbox = document.getElementById(`delete_${editingExistingGuid}`);
                    if (deleteCheckbox && !deleteCheckbox.checked) {
                        deleteCheckbox.checked = true;
                    }
                }

                pendingPhotos.push({
                    fullImageDataUrl,
                    thumbnailDataUrl,
                    title,
                    sortOrder: Number.isFinite(sortOrder) ? sortOrder : pendingPhotos.length
                });
            }

            syncPhotosJson();
            renderPhotos();
            editor.close();
            fileInput.value = "";
            editingPendingIndex = null;
            editingExistingGuid = null;
        }
    });

    if (!editor) {
        return;
    }

    const openEditor = (imageDataUrl, title, sortOrder) => {
        if (!imageDataUrl) {
            return;
        }

        photoTitleEditor.value = title || "";
        photoSortOrderEditor.value = String(sortOrder ?? 0);
        editor.open(imageDataUrl);
    };

    addButton.addEventListener("click", () => {
        if (getAvailableSlots() <= 0) {
            alert("You can attach up to 4 photos.");
            return;
        }

        editingPendingIndex = null;
        editingExistingGuid = null;
        fileInput.click();
    });

    fileInput.addEventListener("change", () => {
        const file = fileInput.files?.[0];
        if (!file) {
            return;
        }

        const reader = new FileReader();
        reader.onload = (event) => {
            const dataUrl = event.target?.result?.toString() || "";
            openEditor(dataUrl, "", pendingPhotos.length);
        };
        reader.readAsDataURL(file);
    });

    document.querySelectorAll(".existing-review-thumb").forEach((button) => {
        button.addEventListener("click", () => {
            const imageUrl = button.getAttribute("data-full-image-url") || button.getAttribute("src") || "";
            if (!imageUrl) {
                return;
            }

            editingPendingIndex = null;
            editingExistingGuid = button.getAttribute("data-existing-guid");
            const title = button.getAttribute("data-title") || "";
            const sortOrder = Number.parseInt(button.getAttribute("data-sort-order") || "0", 10);
            openEditor(imageUrl, title, Number.isFinite(sortOrder) ? sortOrder : 0);
        });
    });

    editBrushSizeInput.addEventListener("input", () => {
        editBrushSizeValue.textContent = editBrushSizeInput.value;
    });
    editBrushSizeValue.textContent = editBrushSizeInput.value;

    syncPhotosJson();
    renderPhotos();
};