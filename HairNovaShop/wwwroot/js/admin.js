// Admin Panel JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Sidebar toggle
    const sidebarToggle = document.getElementById('sidebarToggle');
    const adminSidebar = document.getElementById('adminSidebar');
    const adminMainContent = document.querySelector('.admin-main-content');
    
    if (sidebarToggle && adminSidebar) {
        sidebarToggle.addEventListener('click', function() {
            adminSidebar.classList.toggle('collapsed');
            
            // Save state to localStorage
            const isCollapsed = adminSidebar.classList.contains('collapsed');
            localStorage.setItem('adminSidebarCollapsed', isCollapsed);
        });
        
        // Restore sidebar state from localStorage
        const savedState = localStorage.getItem('adminSidebarCollapsed');
        if (savedState === 'true') {
            adminSidebar.classList.add('collapsed');
        }
    }
    
    // Admin user dropdown
    const adminUserBtn = document.getElementById('adminUserBtn');
    const adminUserDropdown = document.getElementById('adminUserDropdown');
    const adminUserDropdownContainer = document.querySelector('.admin-user-dropdown');
    
    if (adminUserBtn && adminUserDropdown && adminUserDropdownContainer) {
        adminUserBtn.addEventListener('click', function(e) {
            e.stopPropagation();
            const isOpen = adminUserDropdown.classList.contains('show');
            
            // Close all other dropdowns
            document.querySelectorAll('.admin-dropdown-menu.show').forEach(dropdown => {
                dropdown.classList.remove('show');
            });
            document.querySelectorAll('.admin-user-dropdown.show').forEach(dropdown => {
                dropdown.classList.remove('show');
            });
            
            // Toggle this dropdown
            if (!isOpen) {
                adminUserDropdown.classList.add('show');
                adminUserDropdownContainer.classList.add('show');
            } else {
                adminUserDropdown.classList.remove('show');
                adminUserDropdownContainer.classList.remove('show');
            }
        });
        
        // Close dropdown when clicking outside
        document.addEventListener('click', function(e) {
            if (!adminUserDropdownContainer.contains(e.target)) {
                adminUserDropdown.classList.remove('show');
                adminUserDropdownContainer.classList.remove('show');
            }
        });
    }
    
    // Mobile sidebar toggle
    if (window.innerWidth <= 768) {
        if (sidebarToggle && adminSidebar) {
            sidebarToggle.addEventListener('click', function() {
                adminSidebar.classList.toggle('show');
            });
            
            // Close sidebar when clicking outside on mobile
            adminMainContent?.addEventListener('click', function() {
                if (window.innerWidth <= 768) {
                    adminSidebar.classList.remove('show');
                }
            });
        }
    }
    
    // Handle window resize
    window.addEventListener('resize', function() {
        if (window.innerWidth > 768) {
            adminSidebar?.classList.remove('show');
        }
    });
});
