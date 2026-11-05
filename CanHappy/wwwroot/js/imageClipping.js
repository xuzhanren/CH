// Shared helpers for the image clipping (Cropper.js) dialogs used on all pages.
// Limits come from appsettings: ImageClippingMaxSizeInMB and ImageThumbnailMaxSizeInKB (see _Layout.cshtml).
(() => {
    const config = window.CanHappyImageClippingConfig || {};
    const maxFullImageBytes = Number(config.maxFullImageBytes) > 0 ? Number(config.maxFullImageBytes) : 3 * 1024 * 1024;
    const maxThumbnailBytes = Number(config.maxThumbnailBytes) > 0 ? Number(config.maxThumbnailBytes) : 50 * 1024;

    const MIN_JPEG_QUALITY = 0.5;
    const MAX_JPEG_QUALITY = 0.95;
    const THUMBNAIL_QUALITY = 0.85;
    const THUMBNAIL_WORKING_SIDE = 2000;
    const MIN_THUMBNAIL_SIDE = 120;

    const estimateDataUrlSize = (dataUrl) => {
        const payload = (dataUrl || "").split(",")[1] || "";
        return Math.ceil((payload.length * 3) / 4);
    };

    // JPEG has no alpha channel, so flatten onto white instead of the default black.
    const renderScaled = (sourceCanvas, scale) => {
        const width = Math.max(1, Math.round(sourceCanvas.width * scale));
        const height = Math.max(1, Math.round(sourceCanvas.height * scale));
        const target = document.createElement("canvas");
        target.width = width;
        target.height = height;

        const context = target.getContext("2d");
        context.fillStyle = "#ffffff";
        context.fillRect(0, 0, width, height);
        context.imageSmoothingEnabled = true;
        context.imageSmoothingQuality = "high";
        context.drawImage(sourceCanvas, 0, 0, width, height);
        return target;
    };

    const toJpeg = (canvas, quality) => canvas.toDataURL("image/jpeg", quality);

    // Keeps the crop at its original pixel size (Cropper returns the natural size of the selected area).
    const getCroppedCanvas = (cropper) => {
        const options = { imageSmoothingEnabled: true, imageSmoothingQuality: "high" };
        let canvas = null;

        try {
            canvas = cropper.getCroppedCanvas(options);
        } catch (error) {
            canvas = null;
        }

        if (!canvas || !canvas.width || !canvas.height) {
            // Some browsers refuse very large canvases; retry with a cap instead of failing the save.
            canvas = cropper.getCroppedCanvas({ ...options, maxWidth: 8192, maxHeight: 8192 });
        }

        return canvas;
    };

    // Largest JPEG at the original resolution that fits maxBytes: first lower the quality, and only
    // when even the minimum quality is too big, shrink the pixel size to land as close to maxBytes as possible.
    const buildFullImageDataUrl = (sourceCanvas, options = {}) => {
        const maxBytes = Number(options.maxBytes) > 0 ? Number(options.maxBytes) : maxFullImageBytes;

        if (!sourceCanvas || !sourceCanvas.width || !sourceCanvas.height) {
            return "";
        }

        if (options.preferPng) {
            const pngDataUrl = sourceCanvas.toDataURL("image/png");
            if (estimateDataUrlSize(pngDataUrl) <= maxBytes) {
                return pngDataUrl;
            }
        }

        const fullCanvas = renderScaled(sourceCanvas, 1);

        const best = toJpeg(fullCanvas, MAX_JPEG_QUALITY);
        if (estimateDataUrlSize(best) <= maxBytes) {
            return best;
        }

        const lowest = toJpeg(fullCanvas, MIN_JPEG_QUALITY);
        if (estimateDataUrlSize(lowest) <= maxBytes) {
            let low = MIN_JPEG_QUALITY;
            let high = MAX_JPEG_QUALITY;
            let result = lowest;
            for (let i = 0; i < 7; i += 1) {
                const middle = (low + high) / 2;
                const candidate = toJpeg(fullCanvas, middle);
                if (estimateDataUrlSize(candidate) <= maxBytes) {
                    result = candidate;
                    low = middle;
                } else {
                    high = middle;
                }
            }
            return result;
        }

        // Pixel count drives file size, so bisect the scale at a fixed decent quality.
        const quality = 0.7;
        let lowScale = 0.05;
        let highScale = Math.min(1, Math.sqrt(maxBytes / estimateDataUrlSize(lowest)) * 1.3);
        let result = toJpeg(renderScaled(sourceCanvas, lowScale), quality);
        for (let i = 0; i < 8; i += 1) {
            const middle = (lowScale + highScale) / 2;
            const candidate = toJpeg(renderScaled(sourceCanvas, middle), quality);
            if (estimateDataUrlSize(candidate) <= maxBytes) {
                result = candidate;
                lowScale = middle;
            } else {
                highScale = middle;
            }
        }

        return result;
    };

    // Thumbnail whose size is as close to maxBytes (without exceeding it) as possible.
    const buildThumbnailDataUrl = (sourceCanvas, options = {}) => {
        const maxBytes = Number(options.maxBytes) > 0 ? Number(options.maxBytes) : maxThumbnailBytes;

        if (!sourceCanvas || !sourceCanvas.width || !sourceCanvas.height) {
            return "";
        }

        const longestSide = Math.max(sourceCanvas.width, sourceCanvas.height);
        const workingCanvas = renderScaled(sourceCanvas, Math.min(1, THUMBNAIL_WORKING_SIDE / longestSide));

        const sharp = toJpeg(workingCanvas, MAX_JPEG_QUALITY);
        if (estimateDataUrlSize(sharp) <= maxBytes) {
            return sharp;
        }

        const minScale = Math.min(1, MIN_THUMBNAIL_SIDE / Math.max(workingCanvas.width, workingCanvas.height));
        let lowScale = minScale;
        let highScale = 1;
        let result = toJpeg(renderScaled(workingCanvas, lowScale), THUMBNAIL_QUALITY);
        for (let i = 0; i < 9; i += 1) {
            const middle = (lowScale + highScale) / 2;
            const candidate = toJpeg(renderScaled(workingCanvas, middle), THUMBNAIL_QUALITY);
            if (estimateDataUrlSize(candidate) <= maxBytes) {
                result = candidate;
                lowScale = middle;
            } else {
                highScale = middle;
            }
        }

        return result;
    };

    const DEFAULT_IDS = {
        cropperImage: "cropperImage",
        editCanvas: "editCanvas",
        aspectSelect: "cropAspectRatioSelect",
        cropButton: "editCropModeButton",
        penButton: "editPenModeButton",
        eraseButton: "editEraseModeButton",
        textButton: "editTextModeButton",
        fontSelect: "editFontSelect",
        fontSizeInput: "editFontSizeInput",
        brushSizeInput: "editBrushSizeInput",
        colorInput: "editColorInput",
        undoButton: "undoEditButton",
        redoButton: "redoEditButton",
        saveButton: "saveCroppedImageButton"
    };

    // Used by modals whose element ids share a prefix, e.g. "salesSpecials" -> salesSpecialsPenModeButton.
    const PREFIXED_ID_SUFFIXES = {
        cropperImage: "CropperImage",
        editCanvas: "EditCanvas",
        aspectSelect: "AspectRatioSelect",
        cropButton: "CropModeButton",
        penButton: "PenModeButton",
        eraseButton: "EraseModeButton",
        textButton: "TextModeButton",
        fontSelect: "FontSelect",
        fontSizeInput: "FontSizeInput",
        brushSizeInput: "BrushSizeInput",
        colorInput: "ColorInput",
        undoButton: "UndoEditButton",
        redoButton: "RedoEditButton",
        saveButton: "SaveCroppedImageButton"
    };

    const parseAspectRatio = (value) => {
        if (value === "custom") {
            return Number.NaN;
        }

        const parts = (value || "").split(":");
        const width = Number(parts[0]);
        const height = Number(parts[1]);
        if (parts.length !== 2 || !Number.isFinite(width) || !Number.isFinite(height) || width <= 0 || height <= 0) {
            return 3 / 2;
        }

        return width / height;
    };

    const isRemoteUrl = (value) => /^https?:/i.test(value || "");

    // A cached copy fetched without CORS headers would make a crossOrigin request fail, so bust the cache.
    const withCacheBuster = (url) => `${url}${url.includes("?") ? "&" : "?"}cb=${Date.now()}`;

    const loadImage = (source) => new Promise((resolve, reject) => {
        const attempt = (useCors) => {
            const image = new Image();
            if (useCors) {
                image.crossOrigin = "anonymous";
            }

            image.onload = () => resolve({ image, tainted: isRemoteUrl(source) && !useCors });
            image.onerror = () => {
                if (useCors) {
                    attempt(false);
                } else {
                    reject(new Error("Could not load the image."));
                }
            };
            image.src = isRemoteUrl(source) && useCors ? withCacheBuster(source) : source;
        };

        attempt(isRemoteUrl(source));
    });

    const drawOperation = (context, operation) => {
        context.save();
        if (operation.type === "text") {
            context.globalCompositeOperation = "source-over";
            context.fillStyle = operation.color;
            context.font = `${operation.size}px ${operation.font}`;
            context.textBaseline = "alphabetic";
            context.fillText(operation.text, operation.x, operation.y);
        } else {
            context.globalCompositeOperation = operation.mode === "erase" ? "destination-out" : "source-over";
            context.strokeStyle = operation.color;
            context.fillStyle = operation.color;
            context.lineWidth = operation.size;
            context.lineCap = "round";
            context.lineJoin = "round";
            const points = operation.points;
            context.beginPath();
            if (points.length === 1) {
                context.arc(points[0].x, points[0].y, operation.size / 2, 0, Math.PI * 2);
                context.fill();
            } else {
                context.moveTo(points[0].x, points[0].y);
                for (let index = 1; index < points.length; index += 1) {
                    context.lineTo(points[index].x, points[index].y);
                }
                context.stroke();
            }
        }

        context.restore();
    };

    const drawSegment = (context, operation, from, to) => {
        context.save();
        context.globalCompositeOperation = operation.mode === "erase" ? "destination-out" : "source-over";
        context.strokeStyle = operation.color;
        context.lineWidth = operation.size;
        context.lineCap = "round";
        context.lineJoin = "round";
        context.beginPath();
        context.moveTo(from.x, from.y);
        context.lineTo(to.x, to.y);
        context.stroke();
        context.restore();
    };

    // Image clipping + editing dialog shared by all pages.
    //
    // The edits (pen, eraser, text) are kept as a list of operations drawn on a transparent overlay above
    // the untouched original image. That makes undo/redo exact and unlimited, lets the eraser remove only
    // the marks (never the photo), and means switching tools can never clear earlier edits.
    //
    // options: { modal, idPrefix?, ids?, onSave({ canvas, editor }) }
    const createEditor = (options) => {
        const modalElement = typeof options.modal === "string" ? document.getElementById(options.modal) : options.modal;
        if (!modalElement) {
            console.error("Image editor: modal element not found.");
            return null;
        }

        if (typeof Cropper === "undefined") {
            console.error("Image editor: Cropper.js is not loaded.");
            return null;
        }

        const ids = { ...DEFAULT_IDS };
        if (options.idPrefix) {
            Object.keys(PREFIXED_ID_SUFFIXES).forEach((key) => {
                ids[key] = options.idPrefix + PREFIXED_ID_SUFFIXES[key];
            });
        }
        Object.assign(ids, options.ids || {});

        const el = {};
        Object.keys(ids).forEach((key) => {
            el[key] = modalElement.querySelector(`#${ids[key]}`) || document.getElementById(ids[key]);
        });

        if (!el.cropperImage || !el.editCanvas || !el.saveButton) {
            console.error("Image editor: required elements are missing.", ids);
            return null;
        }

        const editContext = el.editCanvas.getContext("2d");
        const container = el.editCanvas.parentElement;

        let mode = "crop";
        let baseImage = null;
        let baseSource = "";
        let loading = Promise.resolve();
        let overlay = null;
        let overlayContext = null;
        let operations = [];
        let undone = [];
        let stroke = null;
        let cropper = null;
        let cropData = null;
        let cropperUrl = null;
        let aspectRatio = parseAspectRatio(el.aspectSelect?.value);
        let token = 0;
        let shown = false;
        let hiding = false;
        let busy = false;

        const toolButtons = [
            [el.cropButton, "crop"],
            [el.penButton, "pen"],
            [el.eraseButton, "erase"],
            [el.textButton, "text"]
        ];

        const updateToolButtons = () => {
            toolButtons.forEach(([button, toolMode]) => {
                if (!button) {
                    return;
                }

                const isActive = mode === toolMode;
                button.classList.toggle("btn-primary", isActive);
                button.classList.toggle("btn-outline-secondary", !isActive);
            });
            el.editCanvas.style.cursor = mode === "text" ? "text" : "crosshair";
        };

        const updateHistoryButtons = () => {
            if (el.undoButton) {
                el.undoButton.disabled = operations.length === 0;
            }
            if (el.redoButton) {
                el.redoButton.disabled = undone.length === 0;
            }
        };

        const releaseCropperUrl = () => {
            if (cropperUrl) {
                URL.revokeObjectURL(cropperUrl);
                cropperUrl = null;
            }
        };

        const destroyCropper = () => {
            if (cropper) {
                try {
                    cropData = cropper.getData();
                } catch (error) {
                    // keep the previous crop box
                }
                cropper.destroy();
                cropper = null;
            }
        };

        const recomposite = (x = 0, y = 0, width = el.editCanvas.width, height = el.editCanvas.height) => {
            const left = Math.max(0, Math.floor(x));
            const top = Math.max(0, Math.floor(y));
            const right = Math.min(el.editCanvas.width, Math.ceil(x + width));
            const bottom = Math.min(el.editCanvas.height, Math.ceil(y + height));
            if (right <= left || bottom <= top || !baseImage) {
                return;
            }

            editContext.clearRect(left, top, right - left, bottom - top);
            editContext.drawImage(baseImage, left, top, right - left, bottom - top, left, top, right - left, bottom - top);
            editContext.drawImage(overlay, left, top, right - left, bottom - top, left, top, right - left, bottom - top);
        };

        const fitCanvas = () => {
            if (!el.editCanvas.width || !el.editCanvas.height) {
                return;
            }

            const ratio = Math.min(
                Math.max(1, container.clientWidth) / el.editCanvas.width,
                Math.max(1, container.clientHeight) / el.editCanvas.height);
            el.editCanvas.style.width = `${Math.max(1, Math.floor(el.editCanvas.width * ratio))}px`;
            el.editCanvas.style.height = `${Math.max(1, Math.floor(el.editCanvas.height * ratio))}px`;
        };

        const rebuildOverlay = () => {
            overlayContext.clearRect(0, 0, overlay.width, overlay.height);
            operations.forEach((operation) => drawOperation(overlayContext, operation));
        };

        // The image the cropper works on: the original plus all marks, at full resolution.
        const createCropperSource = async () => {
            if (operations.length === 0 && /^data:/i.test(baseSource)) {
                return { url: baseSource, temporary: false };
            }

            try {
                const flat = document.createElement("canvas");
                flat.width = baseImage.naturalWidth;
                flat.height = baseImage.naturalHeight;
                const flatContext = flat.getContext("2d");
                flatContext.drawImage(baseImage, 0, 0);
                flatContext.drawImage(overlay, 0, 0);

                const blob = await new Promise((resolve) => flat.toBlob(resolve, "image/png"));
                if (blob) {
                    return { url: URL.createObjectURL(blob), temporary: true };
                }
                return { url: flat.toDataURL("image/png"), temporary: false };
            } catch (error) {
                // Cross-origin image without CORS access: show it as is (saving will report the problem).
                return { url: baseSource, temporary: false };
            }
        };

        const buildCropper = async (currentToken) => {
            destroyCropper();
            releaseCropperUrl();

            const source = await createCropperSource();
            if (currentToken !== token) {
                if (source.temporary) {
                    URL.revokeObjectURL(source.url);
                }
                return;
            }

            cropperUrl = source.temporary ? source.url : null;
            el.cropperImage.classList.remove("d-none");

            // Register the listeners before assigning src so a cached image can never be missed.
            await new Promise((resolve, reject) => {
                el.cropperImage.addEventListener("load", resolve, { once: true });
                el.cropperImage.addEventListener("error", () => reject(new Error("Could not display the image.")), { once: true });
                el.cropperImage.src = source.url;
            });

            if (currentToken !== token) {
                return;
            }

            const readyEvent = new Promise((resolve) => el.cropperImage.addEventListener("ready", resolve, { once: true }));
            const instance = new Cropper(el.cropperImage, {
                aspectRatio,
                viewMode: 1,
                autoCropArea: 1,
                dragMode: "move"
            });
            cropper = instance;
            if (!instance.ready) {
                await readyEvent;
            }

            if (currentToken !== token || cropper !== instance) {
                return;
            }

            if (cropData) {
                try {
                    instance.setData(cropData);
                } catch (error) {
                    // the crop box simply starts from the default
                }
            }
        };

        const enterCropMode = async () => {
            const currentToken = ++token;
            try {
                await loading;
            } catch (error) {
                return;
            }

            if (currentToken !== token || !baseImage) {
                return;
            }

            mode = "crop";
            updateToolButtons();
            el.editCanvas.classList.add("d-none");
            container.style.display = "";
            await buildCropper(currentToken);
        };

        const enterEditMode = async (tool) => {
            const currentToken = ++token;
            try {
                await loading;
            } catch (error) {
                return;
            }

            if (currentToken !== token || !baseImage) {
                return;
            }

            const cameFromCrop = mode === "crop";
            destroyCropper();
            releaseCropperUrl();

            mode = tool;
            updateToolButtons();
            el.cropperImage.classList.add("d-none");
            el.editCanvas.classList.remove("d-none");
            container.style.display = "flex";
            container.style.alignItems = "center";
            container.style.justifyContent = "center";

            if (cameFromCrop || el.editCanvas.width !== overlay.width || el.editCanvas.height !== overlay.height) {
                el.editCanvas.width = overlay.width;
                el.editCanvas.height = overlay.height;
                recomposite();
            }

            fitCanvas();
        };

        const commitOperation = (operation) => {
            operations.push(operation);
            undone = [];
            updateHistoryButtons();
        };

        const applyHistoryChange = async () => {
            rebuildOverlay();
            updateHistoryButtons();
            if (mode === "crop") {
                const currentToken = ++token;
                await buildCropper(currentToken);
            } else {
                recomposite();
            }
        };

        const undo = async () => {
            if (operations.length === 0 || stroke) {
                return;
            }

            undone.push(operations.pop());
            await applyHistoryChange();
        };

        const redo = async () => {
            if (undone.length === 0 || stroke) {
                return;
            }

            operations.push(undone.pop());
            await applyHistoryChange();
        };

        const getPoint = (event) => {
            const rect = el.editCanvas.getBoundingClientRect();
            const scale = el.editCanvas.width / Math.max(1, rect.width);
            return {
                x: (event.clientX - rect.left) * scale,
                y: (event.clientY - rect.top) * scale,
                scale
            };
        };

        // Sizes are typed in on-screen pixels, so scale them to the full resolution image.
        const readStyle = (point) => {
            const brush = Number.parseFloat(el.brushSizeInput?.value);
            const font = Number.parseInt(el.fontSizeInput?.value, 10);
            return {
                color: el.colorInput?.value || "#ff0000",
                brushSize: Math.max(1, (Number.isFinite(brush) ? brush : 6) * point.scale),
                fontSize: Math.max(8, Number.isFinite(font) ? font : 24) * point.scale,
                fontFamily: el.fontSelect?.value || "Arial, sans-serif"
            };
        };

        const eraseRegion = (from, to, size) => {
            const pad = size / 2 + 2;
            recomposite(Math.min(from.x, to.x) - pad, Math.min(from.y, to.y) - pad, Math.abs(from.x - to.x) + pad * 2, Math.abs(from.y - to.y) + pad * 2);
        };

        el.editCanvas.addEventListener("pointerdown", (event) => {
            if (mode === "crop" || !baseImage || (typeof event.button === "number" && event.button > 0)) {
                return;
            }

            const point = getPoint(event);
            const style = readStyle(point);

            if (mode === "text") {
                event.preventDefault();
                const text = window.prompt("Enter text to add:");
                if (!text) {
                    return;
                }

                const operation = { type: "text", text, x: point.x, y: point.y, size: style.fontSize, font: style.fontFamily, color: style.color };
                commitOperation(operation);
                drawOperation(overlayContext, operation);
                drawOperation(editContext, operation);
                return;
            }

            stroke = { type: "stroke", mode, color: style.color, size: style.brushSize, points: [{ x: point.x, y: point.y }] };
            drawOperation(overlayContext, stroke);
            if (mode === "erase") {
                eraseRegion(point, point, stroke.size);
            } else {
                drawOperation(editContext, stroke);
            }

            try {
                el.editCanvas.setPointerCapture(event.pointerId);
            } catch (error) {
                // pointer capture is optional
            }
        });

        el.editCanvas.addEventListener("pointermove", (event) => {
            if (!stroke) {
                return;
            }

            const last = stroke.points[stroke.points.length - 1];
            const next = getPoint(event);
            stroke.points.push({ x: next.x, y: next.y });
            drawSegment(overlayContext, stroke, last, next);
            if (stroke.mode === "erase") {
                eraseRegion(last, next, stroke.size);
            } else {
                drawSegment(editContext, stroke, last, next);
            }
        });

        const finishStroke = (event) => {
            if (!stroke) {
                return;
            }

            const finished = stroke;
            stroke = null;
            commitOperation(finished);
            try {
                el.editCanvas.releasePointerCapture(event.pointerId);
            } catch (error) {
                // already released
            }
        };

        el.editCanvas.addEventListener("pointerup", finishStroke);
        el.editCanvas.addEventListener("pointercancel", finishStroke);

        el.cropButton?.addEventListener("click", () => enterCropMode());
        el.penButton?.addEventListener("click", () => enterEditMode("pen"));
        el.eraseButton?.addEventListener("click", () => enterEditMode("erase"));
        el.textButton?.addEventListener("click", () => enterEditMode("text"));
        el.undoButton?.addEventListener("click", () => undo());
        el.redoButton?.addEventListener("click", () => redo());

        el.aspectSelect?.addEventListener("change", () => {
            aspectRatio = parseAspectRatio(el.aspectSelect.value);
            cropData = null;
            if (cropper) {
                cropper.setAspectRatio(aspectRatio);
            }
        });

        el.saveButton.addEventListener("click", async () => {
            if (busy) {
                return;
            }

            busy = true;
            try {
                // Saving from a drawing tool crops with the last crop box, so edits never block saving.
                if (mode !== "crop" || !cropper) {
                    await enterCropMode();
                }

                if (!cropper) {
                    return;
                }

                const canvas = getCroppedCanvas(cropper);
                if (!canvas || !canvas.width || !canvas.height) {
                    throw new Error("Nothing to save.");
                }

                await options.onSave({ canvas, editor: api });
            } catch (error) {
                console.error(error);
                window.alert("The image could not be saved. If it is stored on another site, reload the page and try again.");
            } finally {
                busy = false;
            }
        });

        modalElement.addEventListener("shown.bs.modal", () => {
            shown = true;
            enterCropMode();
        });

        modalElement.addEventListener("hide.bs.modal", () => {
            shown = false;
            hiding = true;
            token += 1;
            stroke = null;
            destroyCropper();
            releaseCropperUrl();
        });

        modalElement.addEventListener("hidden.bs.modal", () => {
            hiding = false;
        });

        window.addEventListener("resize", () => {
            if (shown && mode !== "crop") {
                fitCanvas();
            }
        });

        const open = (source, openOptions = {}) => {
            token += 1;
            stroke = null;
            destroyCropper();
            releaseCropperUrl();
            cropData = null;
            operations = [];
            undone = [];
            baseImage = null;
            baseSource = source || "";
            mode = "crop";

            if (openOptions.aspectRatio && el.aspectSelect) {
                el.aspectSelect.value = openOptions.aspectRatio;
            }
            aspectRatio = parseAspectRatio(el.aspectSelect?.value);

            updateToolButtons();
            updateHistoryButtons();
            el.editCanvas.classList.add("d-none");
            container.style.display = "";

            loading = loadImage(baseSource).then(({ image }) => {
                baseImage = image;
                overlay = document.createElement("canvas");
                overlay.width = image.naturalWidth;
                overlay.height = image.naturalHeight;
                overlayContext = overlay.getContext("2d");
                el.editCanvas.width = overlay.width;
                el.editCanvas.height = overlay.height;
            });
            loading.catch(() => window.alert("The image could not be loaded."));

            if (shown) {
                enterCropMode();
            } else if (hiding) {
                // Bootstrap ignores show() during the hide transition, so wait for it to finish.
                modalElement.addEventListener("hidden.bs.modal", () => bootstrap.Modal.getOrCreateInstance(modalElement).show(), { once: true });
            } else {
                bootstrap.Modal.getOrCreateInstance(modalElement).show();
            }
        };

        const close = () => {
            bootstrap.Modal.getInstance(modalElement)?.hide();
        };

        const api = { open, close, undo, redo };
        updateToolButtons();
        updateHistoryButtons();
        return api;
    };

    window.CanHappyImageClipping = {
        maxFullImageBytes,
        maxThumbnailBytes,
        estimateDataUrlSize,
        getCroppedCanvas,
        buildFullImageDataUrl,
        buildThumbnailDataUrl,
        createEditor
    };
})();
