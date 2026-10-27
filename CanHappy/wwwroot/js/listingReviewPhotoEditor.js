window.initListingReviewPhotoEditor = (options) => {
    const MAX_PHOTOS = 4;
    const MAX_FULL_IMAGE_BYTES = 1 * 1024 * 1024;
    const MAX_THUMBNAIL_BYTES = 50 * 1024;

    const addButton = document.getElementById(options.addButtonId);
    const fileInput = document.getElementById(options.fileInputId);
    const previewList = document.getElementById(options.previewListId);
    const photosJsonInput = document.getElementById(options.photosJsonInputId);

    const cropModalElement = document.getElementById("reviewPhotoCropModal");
    const cropperImage = document.getElementById("cropperImage");
    const editCanvas = document.getElementById("editCanvas");
    const editCanvasContainer = editCanvas?.parentElement;
    const editContext = editCanvas?.getContext("2d");

    if (!addButton || !fileInput || !previewList || !photosJsonInput || !cropModalElement || !cropperImage || !editCanvas || !editContext) {
        return;
    }

    const photoTitleEditor = document.getElementById("photoTitleEditor");
    const photoSortOrderEditor = document.getElementById("photoSortOrderEditor");
    const cropAspectRatioSelect = document.getElementById("cropAspectRatioSelect");
    const editCropModeButton = document.getElementById("editCropModeButton");
    const editPenModeButton = document.getElementById("editPenModeButton");
    const editEraseModeButton = document.getElementById("editEraseModeButton");
    const editTextModeButton = document.getElementById("editTextModeButton");
    const editFontSelect = document.getElementById("editFontSelect");
    const editFontSizeInput = document.getElementById("editFontSizeInput");
    const editBrushSizeInput = document.getElementById("editBrushSizeInput");
    const editBrushSizeValue = document.getElementById("editBrushSizeValue");
    const editColorInput = document.getElementById("editColorInput");
    const applyEditsButton = document.getElementById("applyEditsButton");
    const undoEditButton = document.getElementById("undoEditButton");
    const redoEditButton = document.getElementById("redoEditButton");
    const saveCroppedImageButton = document.getElementById("saveCroppedImageButton");

    if (!photoTitleEditor || !photoSortOrderEditor || !cropAspectRatioSelect || !editCropModeButton || !editPenModeButton
        || !editEraseModeButton || !editTextModeButton || !editFontSelect || !editFontSizeInput || !editBrushSizeInput
        || !editBrushSizeValue || !editColorInput || !applyEditsButton || !undoEditButton || !redoEditButton || !saveCroppedImageButton) {
        return;
    }

    const cropModal = new bootstrap.Modal(cropModalElement);

    let pendingPhotos = [];
    let sourceImageDataUrl = "";
    let workingImageDataUrl = "";
    let cropper = null;
    let currentAspectRatio = 3 / 2;
    let activeEditTool = "crop";
    let isDrawing = false;
    let lastPoint = null;
    let editHistory = [];
    let redoHistory = [];

    let editingPendingIndex = null;
    let editingExistingGuid = null;

    const estimateDataUrlSize = (dataUrl) => {
        const payload = (dataUrl || "").split(",")[1] || "";
        return Math.ceil((payload.length * 3) / 4);
    };

    const getAspectRatio = (value) => {
        if (value === "custom") {
            return Number.NaN;
        }

        const parts = (value || "").split(":");
        if (parts.length !== 2) {
            return 3 / 2;
        }

        const width = Number(parts[0]);
        const height = Number(parts[1]);
        if (!Number.isFinite(width) || !Number.isFinite(height) || width <= 0 || height <= 0) {
            return 3 / 2;
        }

        return width / height;
    };

    const getCanvasSize = (aspectRatio) => {
        if (Math.abs(aspectRatio - (16 / 9)) < 0.001) {
            return { width: 1280, height: 720 };
        }

        const base = 800;
        if (aspectRatio >= 1) {
            return { width: base, height: Math.round(base / aspectRatio) };
        }

        return { width: Math.round(base * aspectRatio), height: base };
    };

    const buildCroppedImageDataUrl = (sourceCanvas, maxBytes, minDimension = 640) => {
        let width = sourceCanvas.width;
        let height = sourceCanvas.height;
        let quality = 0.9;

        const workCanvas = document.createElement("canvas");
        const workContext = workCanvas.getContext("2d");
        if (!workContext || !width || !height) {
            return sourceCanvas.toDataURL("image/jpeg", quality);
        }

        const render = () => {
            workCanvas.width = width;
            workCanvas.height = height;
            workContext.clearRect(0, 0, width, height);
            workContext.drawImage(sourceCanvas, 0, 0, width, height);
        };

        render();
        let dataUrl = workCanvas.toDataURL("image/jpeg", quality);
        let guard = 0;

        while (estimateDataUrlSize(dataUrl) > maxBytes && guard < 24) {
            guard += 1;
            if (quality > 0.45) {
                quality -= 0.07;
            } else {
                width = Math.max(minDimension, Math.round(width * 0.9));
                height = Math.max(minDimension, Math.round(height * 0.9));
            }

            render();
            dataUrl = workCanvas.toDataURL("image/jpeg", quality);
        }

        return dataUrl;
    };

    const buildThumbnailDataUrl = (sourceCanvas) => buildCroppedImageDataUrl(sourceCanvas, MAX_THUMBNAIL_BYTES, 160);

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

    const createCropper = () => {
        cropper?.destroy();
        cropper = new Cropper(cropperImage, {
            aspectRatio: currentAspectRatio,
            viewMode: 1,
            autoCropArea: 1,
            dragMode: "move"
        });
    };

    const setToolButtonStyles = () => {
        const entries = [
            [editCropModeButton, "crop"],
            [editPenModeButton, "pen"],
            [editEraseModeButton, "erase"],
            [editTextModeButton, "text"]
        ];

        entries.forEach(([button, mode]) => {
            const isActive = activeEditTool === mode;
            button.classList.toggle("btn-primary", isActive);
            button.classList.toggle("btn-outline-secondary", !isActive);
        });
    };

    const fitCanvasDisplaySize = () => {
        if (!editCanvasContainer || !editCanvas.width || !editCanvas.height) {
            return;
        }

        const containerWidth = Math.max(1, editCanvasContainer.clientWidth);
        const containerHeight = Math.max(1, editCanvasContainer.clientHeight);
        const widthRatio = containerWidth / editCanvas.width;
        const heightRatio = containerHeight / editCanvas.height;
        const fitRatio = Math.min(widthRatio, heightRatio);

        const displayWidth = Math.max(1, Math.floor(editCanvas.width * fitRatio));
        const displayHeight = Math.max(1, Math.floor(editCanvas.height * fitRatio));
        editCanvas.style.width = `${displayWidth}px`;
        editCanvas.style.height = `${displayHeight}px`;
    };

    const loadImageToCanvas = (imageDataUrl, onLoaded = null) => {
        if (!imageDataUrl) {
            return;
        }

        const image = new Image();
        image.onload = () => {
            editCanvas.width = image.naturalWidth;
            editCanvas.height = image.naturalHeight;
            editContext.clearRect(0, 0, editCanvas.width, editCanvas.height);
            editContext.drawImage(image, 0, 0, editCanvas.width, editCanvas.height);
            fitCanvasDisplaySize();
            if (typeof onLoaded === "function") {
                onLoaded();
            }
        };
        image.src = imageDataUrl;
    };

    const pushCanvasHistory = () => {
        if (!editCanvas.width || !editCanvas.height) {
            return;
        }

        const snapshot = editCanvas.toDataURL("image/png");
        if (editHistory.length > 0 && editHistory[editHistory.length - 1] === snapshot) {
            return;
        }

        editHistory.push(snapshot);
        if (editHistory.length > 30) {
            editHistory.shift();
        }
    };

    const registerNewEdit = () => {
        pushCanvasHistory();
        redoHistory = [];
    };

    const undoCanvasEdit = () => {
        if (editHistory.length <= 1) {
            return;
        }

        redoHistory.push(editHistory[editHistory.length - 1]);
        editHistory.pop();
        loadImageToCanvas(editHistory[editHistory.length - 1]);
    };

    const redoCanvasEdit = () => {
        if (redoHistory.length === 0) {
            return;
        }

        const snapshot = redoHistory.pop();
        if (!snapshot) {
            return;
        }

        editHistory.push(snapshot);
        loadImageToCanvas(snapshot);
    };

    const showCropperImage = () => {
        editCanvas.classList.add("d-none");
        cropperImage.classList.remove("d-none");
        cropperImage.src = workingImageDataUrl || sourceImageDataUrl;

        if (cropperImage.complete) {
            createCropper();
        } else {
            cropperImage.addEventListener("load", () => createCropper(), { once: true });
        }
    };

    const applyCanvasEdits = () => {
        if (editCanvas.classList.contains("d-none")) {
            return;
        }

        workingImageDataUrl = editCanvas.toDataURL("image/png");
        sourceImageDataUrl = workingImageDataUrl;
    };

    const switchToTool = (tool) => {
        const previousTool = activeEditTool;
        activeEditTool = tool;
        setToolButtonStyles();

        if (tool === "crop") {
            applyCanvasEdits();
            showCropperImage();
            return;
        }

        cropper?.destroy();
        cropper = null;
        cropperImage.classList.add("d-none");
        editCanvas.classList.remove("d-none");

        if (previousTool === "crop" || !editCanvas.width || !editCanvas.height) {
            loadImageToCanvas(workingImageDataUrl || sourceImageDataUrl, () => {
                editHistory = [];
                redoHistory = [];
                pushCanvasHistory();
            });
        }
    };

    const getCanvasPoint = (event) => {
        const rect = editCanvas.getBoundingClientRect();
        const scaleX = editCanvas.width / rect.width;
        const scaleY = editCanvas.height / rect.height;

        return {
            x: (event.clientX - rect.left) * scaleX,
            y: (event.clientY - rect.top) * scaleY
        };
    };

    const drawStroke = (fromPoint, toPoint) => {
        editContext.lineCap = "round";
        editContext.lineJoin = "round";
        const brushSize = Number.parseFloat(editBrushSizeInput.value);
        editContext.lineWidth = Number.isFinite(brushSize) ? brushSize : 6;
        editContext.globalCompositeOperation = activeEditTool === "erase" ? "destination-out" : "source-over";
        editContext.strokeStyle = editColorInput.value || "#ff0000";
        editContext.beginPath();
        editContext.moveTo(fromPoint.x, fromPoint.y);
        editContext.lineTo(toPoint.x, toPoint.y);
        editContext.stroke();
        editContext.globalCompositeOperation = "source-over";
    };

    const openEditor = (imageDataUrl, title, sortOrder) => {
        if (!imageDataUrl) {
            return;
        }

        sourceImageDataUrl = imageDataUrl;
        workingImageDataUrl = sourceImageDataUrl;
        photoTitleEditor.value = title || "";
        photoSortOrderEditor.value = String(sortOrder ?? 0);

        activeEditTool = "crop";
        setToolButtonStyles();
        editHistory = [];
        redoHistory = [];
        editCanvas.width = 0;
        editCanvas.height = 0;

        cropperImage.src = sourceImageDataUrl;
        cropModal.show();
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

    cropModalElement.addEventListener("shown.bs.modal", () => {
        if (!cropperImage.src) {
            return;
        }

        if (activeEditTool === "crop") {
            createCropper();
        } else {
            fitCanvasDisplaySize();
        }
    });

    cropModalElement.addEventListener("hidden.bs.modal", () => {
        cropper?.destroy();
        cropper = null;
    });

    window.addEventListener("resize", () => {
        if (activeEditTool !== "crop" && !editCanvas.classList.contains("d-none")) {
            fitCanvasDisplaySize();
        }
    });

    cropAspectRatioSelect.addEventListener("change", () => {
        currentAspectRatio = getAspectRatio(cropAspectRatioSelect.value);
        if (cropper) {
            cropper.setAspectRatio(currentAspectRatio);
        }
    });

    editCropModeButton.addEventListener("click", () => switchToTool("crop"));
    editPenModeButton.addEventListener("click", () => switchToTool("pen"));
    editEraseModeButton.addEventListener("click", () => switchToTool("erase"));
    editTextModeButton.addEventListener("click", () => switchToTool("text"));
    applyEditsButton.addEventListener("click", () => switchToTool("crop"));
    undoEditButton.addEventListener("click", undoCanvasEdit);
    redoEditButton.addEventListener("click", redoCanvasEdit);

    editBrushSizeInput.addEventListener("input", () => {
        editBrushSizeValue.textContent = editBrushSizeInput.value;
    });
    editBrushSizeValue.textContent = editBrushSizeInput.value;

    editCanvas.addEventListener("pointerdown", (event) => {
        if (activeEditTool === "text") {
            const textValue = prompt("Enter text to add:");
            if (!textValue) {
                return;
            }

            const point = getCanvasPoint(event);
            const fontSize = Number.parseInt(editFontSizeInput.value, 10);
            const resolvedFontSize = Number.isFinite(fontSize) ? Math.max(8, fontSize) : 24;
            const selectedFont = editFontSelect.value || "Arial, sans-serif";
            editContext.globalCompositeOperation = "source-over";
            editContext.fillStyle = editColorInput.value || "#ff0000";
            editContext.font = `${resolvedFontSize}px ${selectedFont}`;
            editContext.fillText(textValue, point.x, point.y);
            registerNewEdit();
            return;
        }

        if (activeEditTool !== "pen" && activeEditTool !== "erase") {
            return;
        }

        isDrawing = true;
        lastPoint = getCanvasPoint(event);
        editCanvas.setPointerCapture(event.pointerId);
    });

    editCanvas.addEventListener("pointermove", (event) => {
        if (!isDrawing || !lastPoint) {
            return;
        }

        const nextPoint = getCanvasPoint(event);
        drawStroke(lastPoint, nextPoint);
        lastPoint = nextPoint;
    });

    const stopDrawing = (event) => {
        if (!isDrawing) {
            return;
        }

        isDrawing = false;
        lastPoint = null;
        registerNewEdit();
        if (typeof event.pointerId !== "undefined") {
            editCanvas.releasePointerCapture(event.pointerId);
        }
    };

    editCanvas.addEventListener("pointerup", stopDrawing);
    editCanvas.addEventListener("pointerleave", stopDrawing);
    editCanvas.addEventListener("pointercancel", stopDrawing);

    saveCroppedImageButton.addEventListener("click", () => {
        if (activeEditTool !== "crop") {
            switchToTool("crop");
        }

        if (!cropper) {
            return;
        }

        const canvas = Number.isFinite(currentAspectRatio)
            ? (() => {
                const canvasSize = getCanvasSize(currentAspectRatio);
                return cropper.getCroppedCanvas({
                    width: canvasSize.width,
                    height: canvasSize.height,
                    imageSmoothingQuality: "high"
                });
            })()
            : cropper.getCroppedCanvas({ imageSmoothingQuality: "high" });

        const fullImageDataUrl = buildCroppedImageDataUrl(canvas, MAX_FULL_IMAGE_BYTES);
        const thumbnailDataUrl = buildThumbnailDataUrl(canvas);
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
        cropModal.hide();
        fileInput.value = "";
        editingPendingIndex = null;
        editingExistingGuid = null;
    });

    setToolButtonStyles();
    syncPhotosJson();
    renderPhotos();
};