// Enhanced CMCS JavaScript

$(document).ready(function () {
    // Hide loading spinner
    $('#loadingSpinner').addClass('hidden');

    // Back to top button
    $(window).scroll(function () {
        if ($(this).scrollTop() > 300) {
            $('#backToTop').addClass('visible');
        } else {
            $('#backToTop').removeClass('visible');
        }
    });

    $('#backToTop').click(function () {
        $('html, body').animate({ scrollTop: 0 }, 500);
        return false;
    });

    // Add animation to cards on scroll
    function animateOnScroll() {
        $('.card, .alert').each(function () {
            const elementTop = $(this).offset().top;
            const elementBottom = elementTop + $(this).outerHeight();
            const viewportTop = $(window).scrollTop();
            const viewportBottom = viewportTop + $(window).height();

            if (elementBottom > viewportTop && elementTop < viewportBottom) {
                $(this).addClass('animate__animated animate__fadeInUp');
            }
        });
    }

    $(window).on('scroll', animateOnScroll);
    animateOnScroll();

    // Enhanced auto-calculation for claim submission
    $('#hoursWorked, #hourlyRate').on('input', function () {
        calculateTotalAmount();
    });

    // Enhanced file upload with preview
    $('.file-upload-input').on('change', function () {
        const fileName = $(this).val().split('\\').pop();
        $('.file-upload-label').html(`<i class="fas fa-file me-2"></i>${fileName || 'Choose file'}`);

        // Show file preview for images
        if (this.files && this.files[0]) {
            const file = this.files[0];
            if (file.type.startsWith('image/')) {
                const reader = new FileReader();
                reader.onload = function (e) {
                    $('.file-preview').html(`<img src="${e.target.result}" class="img-thumbnail" style="max-height: 150px;">`);
                }
                reader.readAsDataURL(file);
            }
        }
    });

    // Enhanced drag and drop
    $('.file-upload-area').on('dragover', function (e) {
        e.preventDefault();
        e.stopPropagation();
        $(this).addClass('dragover bg-primary text-white');
    });

    $('.file-upload-area').on('dragleave', function (e) {
        e.preventDefault();
        e.stopPropagation();
        $(this).removeClass('dragover bg-primary text-white');
    });

    $('.file-upload-area').on('drop', function (e) {
        e.preventDefault();
        e.stopPropagation();
        $(this).removeClass('dragover bg-primary text-white');

        const files = e.originalEvent.dataTransfer.files;
        if (files.length > 0) {
            $('#fileUpload')[0].files = files;
            $('.file-upload-label').html(`<i class="fas fa-file me-2"></i>${files[0].name}`);
        }
    });

    // Enhanced form submission
    $('form').on('submit', function () {
        const submitButton = $(this).find('button[type="submit"]');
        submitButton.prop('disabled', true);
        submitButton.html('<i class="fas fa-spinner fa-spin me-1"></i>Processing...');

        // Add loading state to form
        $(this).addClass('form-loading');
    });

    // Real-time status updates with animation
    if (typeof (connection) !== 'undefined') {
        connection.on("StatusUpdated", function (claimId, newStatus) {
            const statusElement = $(`#status-${claimId}`);
            const badgeElement = $(`#status-badge-${claimId}`);

            statusElement.text(newStatus);
            updateStatusBadge(claimId, newStatus);

            // Add animation
            badgeElement.addClass('animate__animated animate__pulse');
            setTimeout(() => {
                badgeElement.removeClass('animate__animated animate__pulse');
            }, 1000);
        });
    }

    // Notification bell animation
    $('.notification-bell').hover(
        function () {
            $(this).addClass('animate__animated animate__swing');
        },
        function () {
            $(this).removeClass('animate__animated animate__swing');
        }
    );

    // Initialize tooltips
    $('[data-bs-toggle="tooltip"]').tooltip();

    // Auto-dismiss alerts after 5 seconds
    $('.alert').delay(5000).fadeTo(500, 0).slideUp(500, function () {
        $(this).remove();
    });
});

function calculateTotalAmount() {
    const hours = parseFloat($('#hoursWorked').val()) || 0;
    const rate = parseFloat($('#hourlyRate').val()) || 0;
    const total = hours * rate;

    $('#totalAmount').val(total.toFixed(2));
    $('#displayTotal').text('R ' + total.toFixed(2));

    // Add animation to total amount
    $('#displayTotal').addClass('animate__animated animate__pulse');
    setTimeout(() => {
        $('#displayTotal').removeClass('animate__animated animate__pulse');
    }, 500);
}

function updateStatusBadge(claimId, status) {
    const badge = $(`#status-badge-${claimId}`);
    badge.removeClass('bg-warning bg-success bg-danger bg-secondary');

    switch (status.toLowerCase()) {
        case 'approved':
            badge.addClass('bg-success').text('Approved');
            break;
        case 'rejected':
            badge.addClass('bg-danger').text('Rejected');
            break;
        case 'processing':
            badge.addClass('bg-secondary').text('Processing');
            break;
        default:
            badge.addClass('bg-warning').text('Pending');
    }
}

// Enhanced file validation
function validateFile(input) {
    const file = input.files[0];
    const maxSize = 5 * 1024 * 1024; // 5MB
    const allowedTypes = [
        'application/pdf',
        'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
        'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
        'image/jpeg',
        'image/png'
    ];

    if (file) {
        if (file.size > maxSize) {
            showToast('File size must be less than 5MB', 'error');
            input.value = '';
            return false;
        }

        if (!allowedTypes.includes(file.type)) {
            showToast('Please upload PDF, DOCX, XLSX, JPG, or PNG files only', 'error');
            input.value = '';
            return false;
        }
    }

    return true;
}

// Toast notification function
function showToast(message, type = 'info') {
    const toast = $(`
        <div class="toast align-items-center text-white bg-${type} border-0 position-fixed top-0 end-0 m-3" role="alert">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="fas fa-${getToastIcon(type)} me-2"></i>${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>
    `);

    $('.toast-container').append(toast);
    const bsToast = new bootstrap.Toast(toast[0]);
    bsToast.show();

    toast.on('hidden.bs.toast', function () {
        $(this).remove();
    });
}

function getToastIcon(type) {
    const icons = {
        success: 'check-circle',
        error: 'exclamation-triangle',
        warning: 'exclamation-circle',
        info: 'info-circle'
    };
    return icons[type] || 'info-circle';
}

// Initialize toast container if it doesn't exist
if ($('.toast-container').length === 0) {
    $('body').append('<div class="toast-container position-fixed top-0 end-0 p-3" style="z-index: 9999"></div>');
}