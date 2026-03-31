function openAssignModal() {
    // Fetch officers from a new public API endpoint or use a cached list
    $.get('/api/MasterData/officers', function(officers) {
        let options = officers.map(off => `<option value="${off.id}" data-name="${off.fullName}">${off.fullName} (${off.department})</option>`).join('');
        
        Swal.fire({
            title: 'Dispatch Assignment',
            html: `
              <div style="text-align:left;">
                  <div style="margin-bottom:15px;">
                      <label style="display:block; font-size:12px; font-weight:700; margin-bottom:5px;">Acknowledgement No.</label>
                      <input type="text" id="assignAckNo" class="swal2-input" placeholder="e.g. CHD/2026/..." style="margin:0; width:100%;">
                  </div>
                  <div style="margin-bottom:15px;">
                      <label style="display:block; font-size:12px; font-weight:700; margin-bottom:5px;">Select Department Head</label>
                      <select id="assignOfficer" class="swal2-input" style="margin:0; width:100%;">
                          <option value="">-- Choose Officer --</option>
                          ${options}
                      </select>
                  </div>
              </div>
            `,
            showCancelButton: true,
            confirmButtonText: '🚀 Confirm Assignment',
            confirmButtonColor: '#6366f1',
            preConfirm: () => {
                const ack = document.getElementById('assignAckNo').value;
                const offId = document.getElementById('assignOfficer').value;
                const offName = document.getElementById('assignOfficer').options[document.getElementById('assignOfficer').selectedIndex].getAttribute('data-name');
                if (!ack || !offId) {
                    Swal.showValidationMessage('Please provide both Ack No and Officer');
                    return false;
                }
                return { ack, offId, offName };
            }
        }).then((result) => {
            if (result.isConfirmed) {
                performGlobalAssign(result.value.ack, result.value.offId, result.value.offName);
            }
        });
    });
}

function performGlobalAssign(ackNo, officerId, officerName) {
    $.get('/Complaint/GetComplaintByNo', { complaintNo: ackNo })
    .done(function(res) {
        if (res.success) {
            $.post('/Complaint/Reassign', { complaintId: res.complaint.id, headId: officerId, headName: officerName })
            .done(function() {
                Swal.fire({
                    title: 'Assigned!',
                    text: 'Complaint ' + ackNo + ' assigned successfully!',
                    icon: 'success',
                    confirmButtonColor: '#6366f1'
                }).then(() => {
                    // Refresh if on dashboard
                    if (window.location.pathname.includes('Dashboard')) {
                        location.reload();
                    }
                });
            });
        } else {
            Swal.fire('Error', 'Complaint not found.', 'error');
        }
    })
    .fail(function() {
        Swal.fire('Error', 'Invalid complaint number or server error.', 'error');
    });
}
