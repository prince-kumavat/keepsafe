document.addEventListener('DOMContentLoaded', function () {
    const deleteModal = document.getElementById('deleteConfirmModal');
    const deleteForm = document.getElementById('deleteWarrantyForm');
    const closeDeleteBtn = document.getElementById('closeDeleteModal');
    const cancelDeleteBtn = document.getElementById('btnCancelDelete');

    function openDeleteModal(warrantyId) {
        if (!deleteModal || !deleteForm) return;
        deleteForm.action = '/warranties/' + encodeURIComponent(warrantyId) + '/delete';
        deleteModal.classList.add('active');
        document.body.style.overflow = 'hidden';
        cancelDeleteBtn?.focus();
    }

    function closeDeleteModal() {
        if (!deleteModal) return;
        deleteModal.classList.remove('active');
        document.body.style.overflow = '';
    }

    document.querySelectorAll('.btn-delete-warranty').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const id = btn.getAttribute('data-id');
            if (id) {
                openDeleteModal(id);
            }
        });
    });

    closeDeleteBtn?.addEventListener('click', closeDeleteModal);
    cancelDeleteBtn?.addEventListener('click', closeDeleteModal);

    deleteModal?.addEventListener('click', function (e) {
        if (e.target === deleteModal) {
            closeDeleteModal();
        }
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && deleteModal && deleteModal.classList.contains('active')) {
            closeDeleteModal();
        }
    });
});
