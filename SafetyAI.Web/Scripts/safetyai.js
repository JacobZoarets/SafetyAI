// SafetyAI JavaScript functionality

document.addEventListener('DOMContentLoaded', function() {
    initializeUploadArea();
    initializeProgressTracking();
    initializeFormValidation();
});

function initializeUploadArea() {
    const uploadArea = document.getElementById('uploadArea');
    const fileInput = document.querySelector('#fileUpload');
    
    if (!uploadArea || !fileInput) return;

    // Style the file input
    fileInput.style.display = 'none';

    // Prevent default drag behaviors
    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
        uploadArea.addEventListener(eventName, preventDefaults, false);
        document.body.addEventListener(eventName, preventDefaults, false);
    });

    // Highlight drop area when item is dragged over it
    ['dragenter', 'dragover'].forEach(eventName => {
        uploadArea.addEventListener(eventName, highlight, false);
    });

    ['dragleave', 'drop'].forEach(eventName => {
        uploadArea.addEventListener(eventName, unhighlight, false);
    });

    // Handle dropped files
    uploadArea.addEventListener('drop', handleDrop, false);
    
    // Handle click to select file
    uploadArea.addEventListener('click', () => fileInput.click());
    
    // Handle file input change
    fileInput.addEventListener('change', handleFileSelect);

    function preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }

    function highlight(e) {
        uploadArea.classList.add('dragover');
    }

    function unhighlight(e) {
        uploadArea.classList.remove('dragover');
    }

    function handleDrop(e) {
        const dt = e.dataTransfer;
        const files = dt.files;
        handleFiles(files);
    }

    function handleFileSelect(e) {
        const files = e.target.files;
        handleFiles(files);
    }

    function handleFiles(files) {
        if (files.length > 0) {
            const file = files[0];
            
            if (validateFile(file)) {
                updateUploadAreaText(file.name, file.size);
                enableUploadButton();
            }
        }
    }

    function validateFile(file) {
        // Clear previous errors
        clearErrors();
        
        // Validate file type
        const allowedTypes = ['application/pdf', 'image/jpeg', 'image/png', 'image/tiff'];
        const allowedExtensions = ['.pdf', '.jpg', '.jpeg', '.png', '.tiff', '.tif'];
        
        const fileExtension = file.name.toLowerCase().substring(file.name.lastIndexOf('.'));
        
        if (!allowedTypes.includes(file.type) && !allowedExtensions.includes(fileExtension)) {
            showError('Please select a valid file type (PDF, JPEG, PNG, or TIFF).');
            return false;
        }
        
        // Validate file size (10MB limit)
        const maxSize = 10 * 1024 * 1024; // 10MB in bytes
        if (file.size > maxSize) {
            showError('File size must be less than 10MB.');
            return false;
        }
        
        return true;
    }

    function updateUploadAreaText(fileName, fileSize) {
        const uploadContent = uploadArea.querySelector('.upload-content');
        if (uploadContent) {
            const fileSizeText = formatFileSize(fileSize);
            uploadContent.innerHTML = `
                <i class="fas fa-file-alt fa-3x text-success mb-3"></i>
                <h5 class="text-success">File Selected</h5>
                <p><strong>${fileName}</strong></p>
                <p class="text-muted">${fileSizeText}</p>
                <p class="small text-muted">Click "Analyze Report" to process</p>
            `;
        }
    }

    function enableUploadButton() {
        const uploadButton = document.querySelector('#btnUpload');
        if (uploadButton) {
            uploadButton.disabled = false;
            uploadButton.classList.remove('btn-secondary');
            uploadButton.classList.add('btn-primary');
        }
    }

    function formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }
}

function initializeProgressTracking() {
    // Check if we're in a processing state
    const form = document.getElementById('form1');
    if (form) {
        form.addEventListener('submit', function(e) {
            const uploadButton = document.querySelector('#btnUpload');
            if (uploadButton && e.submitter === uploadButton) {
                showProgress();
            }
        });
    }
}

function showProgress() {
    const progressArea = document.getElementById('progressArea');
    const uploadArea = document.getElementById('uploadArea');
    
    if (progressArea) {
        progressArea.style.display = 'block';
        simulateProgress();
    }
    
    if (uploadArea) {
        uploadArea.style.opacity = '0.5';
        uploadArea.style.pointerEvents = 'none';
    }
    
    // Disable upload button
    const uploadButton = document.querySelector('#btnUpload');
    if (uploadButton) {
        uploadButton.disabled = true;
        uploadButton.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Processing...';
    }
}

function simulateProgress() {
    const progressBar = document.querySelector('.progress-bar');
    const progressText = document.querySelector('#progressArea p');
    
    if (!progressBar) return;
    
    let progress = 0;
    const steps = [
        { progress: 20, text: 'Validating file format...' },
        { progress: 40, text: 'Extracting text content...' },
        { progress: 60, text: 'Analyzing safety content...' },
        { progress: 80, text: 'Generating recommendations...' },
        { progress: 95, text: 'Finalizing results...' },
        { progress: 100, text: 'Analysis complete!' }
    ];
    
    let currentStep = 0;
    
    const interval = setInterval(() => {
        if (currentStep < steps.length) {
            const step = steps[currentStep];
            progress = step.progress;
            
            progressBar.style.width = progress + '%';
            progressBar.setAttribute('aria-valuenow', progress);
            
            if (progressText) {
                progressText.textContent = step.text;
            }
            
            currentStep++;
            
            if (progress >= 100) {
                clearInterval(interval);
                // Redirect to results page after a short delay
                setTimeout(() => {
                    window.location.href = 'Results.aspx';
                }, 1000);
            }
        }
    }, 800);
}

function initializeFormValidation() {
    // Real-time validation for forms
    const forms = document.querySelectorAll('form');
    forms.forEach(form => {
        form.addEventListener('submit', function(e) {
            if (!validateForm(form)) {
                e.preventDefault();
                return false;
            }
        });
    });
}

function validateForm(form) {
    let isValid = true;
    
    // Validate file upload if present
    const fileInput = form.querySelector('input[type="file"]');
    if (fileInput && fileInput.files.length === 0) {
        showError('Please select a file to upload.');
        isValid = false;
    }
    
    return isValid;
}

function showError(message) {
    clearErrors();
    
    const uploadArea = document.getElementById('uploadArea');
    if (uploadArea) {
        const errorDiv = document.createElement('div');
        errorDiv.className = 'alert alert-danger mt-3';
        errorDiv.id = 'fileError';
        errorDiv.innerHTML = `<i class="fas fa-exclamation-triangle me-2"></i>${message}`;
        
        uploadArea.parentNode.appendChild(errorDiv);
        
        // Auto-hide error after 5 seconds
        setTimeout(() => {
            clearErrors();
        }, 5000);
    }
}

function clearErrors() {
    const existingError = document.getElementById('fileError');
    if (existingError) {
        existingError.remove();
    }
}

// Utility functions
function showToast(message, type = 'info') {
    // Create toast notification
    const toast = document.createElement('div');
    toast.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
    toast.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
    toast.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    
    document.body.appendChild(toast);
    
    // Auto-remove after 5 seconds
    setTimeout(() => {
        if (toast.parentNode) {
            toast.parentNode.removeChild(toast);
        }
    }, 5000);
}

// Auto-refresh functionality for processing status
function checkProcessingStatus() {
    const statusElements = document.querySelectorAll('[data-status]');
    statusElements.forEach(element => {
        const status = element.getAttribute('data-status');
        if (status === 'Processing' || status === 'Pending') {
            // Refresh page every 5 seconds if processing
            setTimeout(() => {
                window.location.reload();
            }, 5000);
        }
    });
}

// Initialize status checking
checkProcessingStatus();

// Export functions for global access
window.SafetyAI = {
    showError: showError,
    clearErrors: clearErrors,
    showToast: showToast,
    showProgress: showProgress
};