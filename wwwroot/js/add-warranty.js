document.addEventListener('DOMContentLoaded', function () {
    const addModal = document.getElementById('addWarrantyModal');
    const openAddBtn = document.getElementById('openAddWarrantyBtn');
    const emptyStateAddBtn = document.getElementById('emptyStateAddWarrantyBtn');
    const closeAddBtn = document.getElementById('closeAddWarrantyModal');
    const cancelAddBtn = document.getElementById('btnCancelAddWarranty');
    const form = document.getElementById('addWarrantyForm');
    const productNameInput = document.getElementById('productNameInput');
    const expirationDateInput = document.getElementById('expirationDateInput');
    const fileInput = document.getElementById('fileUploadInput');
    const dropZone = document.getElementById('dropZone');
    const filePreviewArea = document.getElementById('filePreviewArea');
    const imageThumbnail = document.getElementById('imageThumbnail');
    const pdfIconWrap = document.getElementById('pdfIconWrap');
    const previewFileName = document.getElementById('previewFileName');
    const previewFileSize = document.getElementById('previewFileSize');
    const removeFileBtn = document.getElementById('btnRemoveFile');
    const submitBtn = document.getElementById('btnSubmitAddWarranty');
    const uploadProgressWrap = document.getElementById('uploadProgressWrap');
    const progressBarFill = document.getElementById('progressBarFill');
    const progressStatusText = document.getElementById('progressStatusText');
    const errorAlert = document.getElementById('addWarrantyErrorAlert');
    const errorMessage = document.getElementById('addWarrantyErrorMessage');

    let currentObjectUrl = null;
    let isUploading = false;

    function openModal() {
        if (!addModal) return;
        resetForm();
        addModal.classList.add('active');
        document.body.style.overflow = 'hidden';
        productNameInput?.focus();
    }

    function closeModal() {
        if (!addModal || isUploading) return;
        addModal.classList.remove('active');
        document.body.style.overflow = '';
        revokeThumbnail();
    }

    function revokeThumbnail() {
        if (currentObjectUrl) {
            URL.revokeObjectURL(currentObjectUrl);
            currentObjectUrl = null;
        }
    }

    function showError(msg) {
        if (!errorAlert || !errorMessage) return;
        errorMessage.innerText = msg;
        errorAlert.style.display = 'block';
    }

    function clearError() {
        if (!errorAlert) return;
        errorAlert.style.display = 'none';
    }

    function resetForm() {
        if (form) form.reset();
        clearError();
        revokeThumbnail();
        if (filePreviewArea) filePreviewArea.style.display = 'none';
        if (dropZone) dropZone.style.display = 'block';
        if (uploadProgressWrap) uploadProgressWrap.style.display = 'none';
        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.innerText = 'SAVE WARRANTY';
        }
        isUploading = false;
        enableFormControls(true);
    }

    function enableFormControls(enabled) {
        if (productNameInput) productNameInput.disabled = !enabled;
        if (expirationDateInput) expirationDateInput.disabled = !enabled;
        if (fileInput) fileInput.disabled = !enabled;
        if (closeAddBtn) closeAddBtn.disabled = !enabled;
        if (cancelAddBtn) cancelAddBtn.disabled = !enabled;
        if (removeFileBtn) removeFileBtn.disabled = !enabled;
    }

    function checkValidity() {
        const hasName = productNameInput && productNameInput.value.trim().length > 0;
        const hasDate = expirationDateInput && expirationDateInput.value.trim().length > 0;
        const hasFile = fileInput && fileInput.files && fileInput.files.length > 0;

        if (submitBtn && !isUploading) {
            submitBtn.disabled = !(hasName && hasDate && hasFile);
        }
    }

    function handleFile(file) {
        clearError();
        if (!file) return;

        // Check 10 MB limit
        const maxBytes = 10 * 1024 * 1024;
        if (file.size > maxBytes) {
            showError('File size exceeds the 10 MB limit.');
            removeSelectedFile();
            return;
        }

        const validTypes = ['image/jpeg', 'image/jpg', 'image/png', 'application/pdf'];
        const fileNameLower = file.name.toLowerCase();
        const validExts = ['.jpg', '.jpeg', '.png', '.pdf'];
        const hasValidExt = validExts.some(ext => fileNameLower.endsWith(ext));

        if (!hasValidExt && !validTypes.includes(file.type)) {
            showError('Invalid file type. Only JPEG, PNG and PDF files are allowed.');
            removeSelectedFile();
            return;
        }

        // Show preview
        revokeThumbnail();
        if (dropZone) dropZone.style.display = 'none';
        if (filePreviewArea) filePreviewArea.style.display = 'block';

        if (previewFileName) previewFileName.innerText = file.name;
        if (previewFileSize) previewFileSize.innerText = (file.size / (1024 * 1024)).toFixed(2) + ' MB';

        if (file.type.startsWith('image/') || fileNameLower.endsWith('.jpg') || fileNameLower.endsWith('.jpeg') || fileNameLower.endsWith('.png')) {
            currentObjectUrl = URL.createObjectURL(file);
            if (imageThumbnail) {
                imageThumbnail.src = currentObjectUrl;
                imageThumbnail.style.display = 'block';
            }
            if (pdfIconWrap) pdfIconWrap.style.display = 'none';
        } else {
            if (imageThumbnail) imageThumbnail.style.display = 'none';
            if (pdfIconWrap) pdfIconWrap.style.display = 'block';
        }

        checkValidity();
    }

    function removeSelectedFile() {
        if (fileInput) fileInput.value = '';
        revokeThumbnail();
        if (filePreviewArea) filePreviewArea.style.display = 'none';
        if (dropZone) dropZone.style.display = 'block';
        checkValidity();
    }

    openAddBtn?.addEventListener('click', openModal);
    emptyStateAddBtn?.addEventListener('click', openModal);
    closeAddBtn?.addEventListener('click', closeModal);
    cancelAddBtn?.addEventListener('click', closeModal);

    productNameInput?.addEventListener('input', checkValidity);
    expirationDateInput?.addEventListener('change', checkValidity);

    removeFileBtn?.addEventListener('click', removeSelectedFile);

    // Drop zone click & drag-and-drop
    dropZone?.addEventListener('click', function () {
        fileInput?.click();
    });

    fileInput?.addEventListener('change', function () {
        if (fileInput.files && fileInput.files[0]) {
            handleFile(fileInput.files[0]);
        }
    });

    ['dragenter', 'dragover'].forEach(eventName => {
        dropZone?.addEventListener(eventName, function (e) {
            e.preventDefault();
            e.stopPropagation();
            dropZone.classList.add('dragover');
        });
    });

    ['dragleave', 'drop'].forEach(eventName => {
        dropZone?.addEventListener(eventName, function (e) {
            e.preventDefault();
            e.stopPropagation();
            dropZone.classList.remove('dragover');
        });
    });

    dropZone?.addEventListener('drop', function (e) {
        const dt = e.dataTransfer;
        if (dt && dt.files && dt.files[0]) {
            fileInput.files = dt.files;
            handleFile(dt.files[0]);
        }
    });

    // Close on backdrop or Escape
    addModal?.addEventListener('click', function (e) {
        if (e.target === addModal && !isUploading) {
            closeModal();
        }
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && addModal && addModal.classList.contains('active') && !isUploading) {
            closeModal();
        }
    });

    // Form submission with XMLHttpRequest for upload progress
    form?.addEventListener('submit', function (e) {
        e.preventDefault();
        clearError();

        if (!productNameInput.value.trim()) {
            showError('Product name is required.');
            return;
        }
        if (!expirationDateInput.value) {
            showError('Expiration date is required.');
            return;
        }
        if (!fileInput.files || !fileInput.files[0]) {
            showError('Please select a file.');
            return;
        }

        const formData = new FormData(form);
        const antiForgeryToken = form.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

        isUploading = true;
        enableFormControls(false);
        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.innerText = 'UPLOADING…';
        }
        if (uploadProgressWrap) {
            uploadProgressWrap.style.display = 'block';
            progressBarFill.style.width = '0%';
            progressStatusText.innerText = 'UPLOADING… 0%';
        }

        const xhr = new XMLHttpRequest();
        xhr.open('POST', '/warranties', true);
        if (antiForgeryToken) {
            xhr.setRequestHeader('RequestVerificationToken', antiForgeryToken);
        }

        xhr.upload.onprogress = function (event) {
            if (event.lengthComputable) {
                const percent = Math.round((event.loaded / event.total) * 100);
                if (progressBarFill) progressBarFill.style.width = percent + '%';
                if (percent < 100) {
                    if (progressStatusText) progressStatusText.innerText = 'UPLOADING… ' + percent + '%';
                } else {
                    if (progressStatusText) progressStatusText.innerText = 'PROCESSING…';
                    if (submitBtn) submitBtn.innerText = 'PROCESSING…';
                }
            }
        };

        xhr.onload = function () {
            isUploading = false;
            try {
                const response = JSON.parse(xhr.responseText);
                if (xhr.status >= 200 && xhr.status < 300 && response.success) {
                    if (progressStatusText) progressStatusText.innerText = 'WARRANTY SAVED!';
                    // Reload page to reflect new warranty and statistics
                    setTimeout(function () {
                        window.location.reload();
                    }, 400);
                } else {
                    enableFormControls(true);
                    if (submitBtn) {
                        submitBtn.disabled = false;
                        submitBtn.innerText = 'SAVE WARRANTY';
                    }
                    if (uploadProgressWrap) uploadProgressWrap.style.display = 'none';
                    showError(response.message || 'An error occurred while uploading.');
                }
            } catch (err) {
                enableFormControls(true);
                if (submitBtn) {
                    submitBtn.disabled = false;
                    submitBtn.innerText = 'SAVE WARRANTY';
                }
                if (uploadProgressWrap) uploadProgressWrap.style.display = 'none';
                showError('Unexpected server response. Please try again.');
            }
        };

        xhr.onerror = function () {
            isUploading = false;
            enableFormControls(true);
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.innerText = 'SAVE WARRANTY';
            }
            if (uploadProgressWrap) uploadProgressWrap.style.display = 'none';
            showError('Network error occurred during upload.');
        };

        xhr.send(formData);
    });
});
