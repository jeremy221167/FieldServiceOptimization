// Fleet Map Google Maps Integration
let map;
let technicians = {};
let incidents = {};
let jobs = {};
let trafficLayer = null;

// Color palette for unique technician identification
const technicianColors = [
    '#FF6B6B', '#4ECDC4', '#45B7D1', '#96CEB4', '#FFEAA7',
    '#DDA0DD', '#98D8C8', '#F7DC6F', '#BB8FCE', '#85C1E9',
    '#F8C471', '#82E0AA', '#F1948A', '#85C1E9', '#D5A6BD'
];

window.initializeFleetMap = (options) => {
    try {
        if (typeof google === 'undefined' || !google.maps) {
            console.error('Google Maps API not loaded');
            return;
        }

        const mapElement = document.getElementById(options.mapId);
        if (!mapElement) {
            console.error('Map element not found:', options.mapId);
            return;
        }

    // Initialize map
    map = new google.maps.Map(mapElement, {
        center: options.center,
        zoom: options.zoom,
        mapTypeId: google.maps.MapTypeId.ROADMAP,
        styles: [
            {
                featureType: "poi",
                elementType: "labels",
                stylers: [{ visibility: "off" }]
            }
        ]
    });

    // Initialize traffic layer
    trafficLayer = new google.maps.TrafficLayer();
    if (options.showTraffic) {
        trafficLayer.setMap(map);
    }

    // Add technician markers
    options.technicians.forEach(tech => {
        addTechnicianMarker(tech);
    });

    // Add incident markers
    options.incidents.forEach(incident => {
        addIncidentMarker(incident);
    });

    // Add job markers
    if (options.jobs) {
        options.jobs.forEach(job => {
            addJobMarker(job);
        });
    }

        console.log('Fleet map initialized successfully');
    } catch (error) {
        console.error('Error initializing fleet map:', error);
    }
};

function addTechnicianMarker(techData) {
    const marker = new google.maps.Marker({
        position: { lat: techData.lat, lng: techData.lng },
        map: map,
        title: techData.name,
        icon: {
            url: getVanIconUrl(techData.status, techData.id),
            scaledSize: new google.maps.Size(36, 36),
            anchor: new google.maps.Point(18, 28)
        }
    });

    // Create info window
    const infoWindow = new google.maps.InfoWindow({
        content: createTechnicianInfoContent(techData)
    });

    marker.addListener('click', () => {
        try {
            // Close other info windows safely
            Object.values(technicians).forEach(t => {
                if (t.infoWindow && t.infoWindow.close) {
                    t.infoWindow.close();
                }
            });
            if (infoWindow && infoWindow.open) {
                infoWindow.open(map, marker);
            }
        } catch (error) {
            // Silently handle click errors
        }
    });

    technicians[techData.id] = {
        marker: marker,
        infoWindow: infoWindow,
        data: techData
    };
}

function addIncidentMarker(incidentData) {
    const marker = new google.maps.Marker({
        position: { lat: incidentData.lat, lng: incidentData.lng },
        map: map,
        title: `${incidentData.type}: ${incidentData.description}`,
        icon: {
            url: getIncidentIconUrl(incidentData.severity),
            scaledSize: new google.maps.Size(24, 24),
            anchor: new google.maps.Point(12, 12)
        }
    });

    const infoWindow = new google.maps.InfoWindow({
        content: createIncidentInfoContent(incidentData)
    });

    marker.addListener('click', () => {
        infoWindow.open(map, marker);
    });

    incidents[incidentData.id] = {
        marker: marker,
        infoWindow: infoWindow,
        data: incidentData
    };
}

function addJobMarker(jobData) {
    const marker = new google.maps.Marker({
        position: { lat: jobData.lat, lng: jobData.lng },
        map: map,
        title: `Job: ${jobData.serviceType} - ${jobData.priority}`,
        icon: {
            url: getJobIconUrl(jobData.priority, jobData.isEmergency),
            scaledSize: new google.maps.Size(28, 28),
            anchor: new google.maps.Point(14, 14)
        }
    });

    const infoWindow = new google.maps.InfoWindow({
        content: createJobInfoContent(jobData)
    });

    marker.addListener('click', () => {
        infoWindow.open(map, marker);
    });

    jobs[jobData.jobId] = {
        marker: marker,
        infoWindow: infoWindow,
        data: jobData
    };
}

function getJobIconUrl(priority, isEmergency) {
    const colors = {
        'Low': '#28a745',
        'Normal': '#17a2b8',
        'High': '#ffc107',
        'Urgent': '#dc3545'
    };

    const color = colors[priority] || '#6c757d';
    const icon = isEmergency ? '🚨' : '📍';

    return `data:image/svg+xml;charset=UTF-8,${encodeURIComponent(`
        <svg width="28" height="28" viewBox="0 0 28 28" xmlns="http://www.w3.org/2000/svg">
            <circle cx="14" cy="14" r="12" fill="${color}" stroke="white" stroke-width="2"/>
            <text x="14" y="18" text-anchor="middle" fill="white" font-size="10" font-family="Arial">${icon}</text>
        </svg>
    `)}`;
}

function createJobInfoContent(jobData) {
    return `
        <div style="min-width: 220px;">
            <h6><strong>Job ${jobData.jobId}</strong></h6>
            <p><strong>Service:</strong> ${jobData.serviceType}</p>
            <p><strong>Location:</strong> ${jobData.location}</p>
            <p><strong>Priority:</strong> <span class="badge" style="background-color: ${getJobPriorityColor(jobData.priority)}">${jobData.priority}</span></p>
            <p><strong>Customer:</strong> ${jobData.customerName}</p>
            <p><strong>Phone:</strong> ${jobData.customerPhone}</p>
            <p><strong>Scheduled:</strong> ${new Date(jobData.scheduledDate).toLocaleString()}</p>
            ${jobData.isEmergency ? '<p><span class="badge bg-danger">🚨 EMERGENCY</span></p>' : ''}
        </div>
    `;
}

function getJobPriorityColor(priority) {
    const colors = {
        'Low': '#28a745',
        'Normal': '#17a2b8',
        'High': '#ffc107',
        'Urgent': '#dc3545'
    };
    return colors[priority] || '#6c757d';
}

function getVanIconUrl(status, technicianId) {
    // Get unique color for this technician
    const techIndex = parseInt(technicianId.replace('TECH', '')) - 1;
    const techColor = technicianColors[techIndex % technicianColors.length];

    // Status-based border colors
    const borderColors = {
        'Available': '#28a745',
        'EnRoute': '#ffc107',
        'OnSite': '#17a2b8',
        'Busy': '#dc3545'
    };
    const borderColor = borderColors[status] || '#6c757d';

    return `data:image/svg+xml;charset=UTF-8,${encodeURIComponent(`
        <svg width="36" height="36" viewBox="0 0 36 36" xmlns="http://www.w3.org/2000/svg">
            <!-- Main van body -->
            <rect x="8" y="14" width="20" height="10" rx="2" fill="${techColor}" stroke="${borderColor}" stroke-width="2"/>
            <!-- Van cab -->
            <rect x="8" y="10" width="8" height="6" rx="1" fill="${techColor}" stroke="${borderColor}" stroke-width="2"/>
            <!-- Wheels -->
            <circle cx="12" cy="26" r="3" fill="#333" stroke="white" stroke-width="1"/>
            <circle cx="24" cy="26" r="3" fill="#333" stroke="white" stroke-width="1"/>
            <!-- Status indicator -->
            <circle cx="30" cy="8" r="5" fill="${borderColor}" stroke="white" stroke-width="2"/>
            <!-- Van number -->
            <text x="18" y="20" text-anchor="middle" fill="white" font-size="8" font-family="Arial" font-weight="bold">${techIndex + 1}</text>
        </svg>
    `)}`;
}

function getIncidentIconUrl(severity) {
    const colors = {
        'Low': '#28a745',
        'Medium': '#ffc107',
        'High': '#fd7e14',
        'Critical': '#dc3545'
    };

    const color = colors[severity] || '#6c757d';
    return `data:image/svg+xml;charset=UTF-8,${encodeURIComponent(`
        <svg width="24" height="24" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <circle cx="12" cy="12" r="10" fill="${color}" stroke="white" stroke-width="2"/>
            <text x="12" y="16" text-anchor="middle" fill="white" font-size="10" font-family="Arial">⚠️</text>
        </svg>
    `)}`;
}

function createTechnicianInfoContent(techData) {
    return `
        <div style="min-width: 200px;">
            <h6><strong>${techData.name}</strong></h6>
            <p><small>ID: ${techData.id}</small></p>
            <p><strong>Status:</strong> <span class="badge" style="background-color: ${getTechnicianColor(techData.status)}">${techData.status}</span></p>
            <p><strong>Location:</strong> ${techData.lat.toFixed(4)}, ${techData.lng.toFixed(4)}</p>
        </div>
    `;
}

function createIncidentInfoContent(incidentData) {
    return `
        <div style="min-width: 200px;">
            <h6><strong>${incidentData.type}</strong></h6>
            <p><strong>Severity:</strong> <span class="badge" style="background-color: ${getIncidentColor(incidentData.severity)}">${incidentData.severity}</span></p>
            <p><strong>Description:</strong> ${incidentData.description}</p>
            <p><strong>Location:</strong> ${incidentData.lat.toFixed(4)}, ${incidentData.lng.toFixed(4)}</p>
        </div>
    `;
}

function getTechnicianColor(status) {
    const colors = {
        'Available': '#28a745',
        'EnRoute': '#ffc107',
        'OnSite': '#17a2b8',
        'Busy': '#dc3545'
    };
    return colors[status] || '#6c757d';
}

function getIncidentColor(severity) {
    const colors = {
        'Low': '#28a745',
        'Medium': '#ffc107',
        'High': '#fd7e14',
        'Critical': '#dc3545'
    };
    return colors[severity] || '#6c757d';
}

// Update functions called from Blazor
window.updateTechnicianMarker = (technicianId, lat, lng) => {
    try {
        const tech = technicians[technicianId];
        if (tech && tech.marker) {
            tech.marker.setPosition({ lat: lat, lng: lng });
            tech.data.lat = lat;
            tech.data.lng = lng;

            // Update info window content
            tech.infoWindow.setContent(createTechnicianInfoContent(tech.data));
        }
    } catch (error) {
        console.error('Error updating technician marker:', error);
    }
};

window.toggleTrafficLayer = (show) => {
    if (trafficLayer) {
        trafficLayer.setMap(show ? map : null);
    }
};

window.toggleIncidentMarkers = (show) => {
    Object.values(incidents).forEach(incident => {
        incident.marker.setVisible(show);
    });
};

window.toggleJobMarkers = (show) => {
    Object.values(jobs).forEach(job => {
        job.marker.setVisible(show);
    });
};

window.refreshMapData = () => {
    // Simplified refresh without marker manipulation
    try {
        technicians = {};
        incidents = {};
        jobs = {};

        console.log('Map data cleared for refresh');
    } catch (error) {
        // Silently ignore refresh errors
    }
};

window.clearFleetMap = () => {
    // Simplified cleanup without DOM manipulation
    try {
        // Just reset the objects, don't manipulate markers
        technicians = {};
        incidents = {};
        jobs = {};
        trafficLayer = null;
        map = null;

        console.log('Fleet map cleared');
    } catch (error) {
        // Silently ignore cleanup errors
    }
};