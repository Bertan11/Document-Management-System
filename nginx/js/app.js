const API_BASE = '/api';  

const uploadForm = document.getElementById('uploadForm');
const fileInput = document.getElementById('fileInput');
const fileName = document.getElementById('fileName');
const uploadBtn = document.getElementById('uploadBtn');
const uploadStatus = document.getElementById('uploadStatus');
const searchInput = document.getElementById('searchInput');
const searchBtn = document.getElementById('searchBtn');
const showAllBtn = document.getElementById('showAllBtn');
const documentsContainer = document.getElementById('documentsContainer');
const loadingSpinner = document.getElementById('loadingSpinner');
const documentModal = document.getElementById('documentModal');
const documentDetails = document.getElementById('documentDetails');


document.addEventListener('DOMContentLoaded', function () {
    loadAllDocuments();
    setupEventListeners();
});

function setupEventListeners() {
    fileInput.addEventListener('change', function () {
        const file = this.files[0];
        if (file) {
            fileName.textContent = `Ausgewählt: ${file.name}`;
        } else {
            fileName.textContent = '';
        }
    });


    uploadForm.addEventListener('submit', handleFileUpload);

   
    searchBtn.addEventListener('click', handleSearch);
    showAllBtn.addEventListener('click', loadAllDocuments);

   
    searchInput.addEventListener('keypress', function (e) {
        if (e.key === 'Enter') {
            handleSearch();
        }
    });

    
    const closeModal = document.querySelector('.close');
    if (closeModal) {
        closeModal.addEventListener('click', function () {
            documentModal.style.display = 'none';
        });
    }

    
    window.addEventListener('click', function (e) {
        if (e.target === documentModal) {
            documentModal.style.display = 'none';
        }
    });
}


async function handleFileUpload(e) {
    e.preventDefault();

    const file = fileInput.files[0];
    if (!file) {
        showStatus('Bitte wählen Sie eine Datei aus', 'error');
        return;
    }

    const formData = new FormData();
    formData.append('file', file);

    try {
        uploadBtn.disabled = true;
        uploadBtn.textContent = 'Hochladen...';
        showStatus('Datei wird hochgeladen...', 'info');

        const response = await fetch(`${API_BASE}/document/upload`, {
            method: 'POST',
            body: formData
        });

        if (response.ok) {
            const result = await response.json();
            showStatus(`Datei "${result.filename}" wurde erfolgreich hochgeladen!`, 'success');
            uploadForm.reset();
            fileName.textContent = '';
            loadAllDocuments(); 
        } else {
            const errorText = await response.text();
            throw new Error(`Upload fehlgeschlagen: ${errorText}`);
        }
    } catch (error) {
        console.error('Upload error:', error);
        showStatus(`Fehler beim Hochladen: ${error.message}`, 'error');
    } finally {
        uploadBtn.disabled = false;
        uploadBtn.textContent = 'Hochladen';
    }
}

// Search Function
async function handleSearch() {
    const query = searchInput.value.trim();

    if (query === '') {
        loadAllDocuments();
        return;
    }

    try {
        showLoading(true);
        const response = await fetch(`${API_BASE}/document/search?query=${encodeURIComponent(query)}`);

        if (response.ok) {
            const documents = await response.json();
            displayDocuments(documents);
        } else {
            throw new Error('Suche fehlgeschlagen');
        }
    } catch (error) {
        console.error('Search error:', error);
        showStatus(`Fehler bei der Suche: ${error.message}`, 'error');
    } finally {
        showLoading(false);
    }
}

// Load All Documents
async function loadAllDocuments() {
    try {
        showLoading(true);
        const response = await fetch(`${API_BASE}/document`);

        if (response.ok) {
            const documents = await response.json();
            displayDocuments(documents);
        } else {
            throw new Error('Fehler beim Laden der Dokumente');
        }
    } catch (error) {
        console.error('Load documents error:', error);
        documentsContainer.innerHTML = '<p class="error">Fehler beim Laden der Dokumente</p>';
    } finally {
        showLoading(false);
    }
}

// Display Documents
function displayDocuments(documents) {
    if (!documents || documents.length === 0) {
        documentsContainer.innerHTML = '<p class="no-documents">Keine Dokumente gefunden.</p>';
        return;
    }

    const documentsGrid = document.createElement('div');
    documentsGrid.className = 'documents-grid';

    documents.forEach(doc => {
        const docCard = createDocumentCard(doc);
        documentsGrid.appendChild(docCard);
    });

    documentsContainer.innerHTML = '';
    documentsContainer.appendChild(documentsGrid);
}

// Create Document Card
function createDocumentCard(doc) {
    const card = document.createElement('div');
    card.className = 'document-card';

    // Format file size
    const fileSize = formatFileSize(doc.fileSize);
    const uploadDate = new Date(doc.uploadDate).toLocaleString('de-DE');

    // Status indicators
    const statusText = doc.isProcessed ? 'Verarbeitet' : 'In Bearbeitung';
    const statusColor = doc.isProcessed ? '#22543d' : '#d69e2e';

    card.innerHTML = `
        <h3>${escapeHtml(doc.filename)}</h3>
        <div class="document-info">
            <span><strong>Typ:</strong> ${doc.contentType}</span>
            <span><strong>Größe:</strong> ${fileSize}</span>
            <span><strong>Hochgeladen:</strong> ${uploadDate}</span>
            <span style="color: ${statusColor}"><strong>Status:</strong> ${statusText}</span>
        </div>
        <div class="document-actions">
            <button class="btn btn-secondary" onclick="viewDocument(${doc.id})">Details</button>
            <button class="btn btn-danger" onclick="deleteDocument(${doc.id})">Löschen</button>
        </div>
    `;

    return card;
}

// View Document Details
async function viewDocument(id) {
    try {
        const response = await fetch(`${API_BASE}/document/${id}`);

        if (response.ok) {
            const doc = await response.json();
            showDocumentModal(doc);
        } else {
            throw new Error('Dokument nicht gefunden');
        }
    } catch (error) {
        console.error('View document error:', error);
        showStatus(`Fehler beim Laden des Dokuments: ${error.message}`, 'error');
    }
}

// Show Document Modal
function showDocumentModal(doc) {
    const fileSize = formatFileSize(doc.fileSize);
    const uploadDate = new Date(doc.uploadDate).toLocaleString('de-DE');
    const processedDate = doc.processedDate ? new Date(doc.processedDate).toLocaleString('de-DE') : 'Noch nicht verarbeitet';

    documentDetails.innerHTML = `
        <h2>Dokumentdetails</h2>
        <div class="detail-row">
            <span class="detail-label">Dateiname:</span>
            <span class="detail-value">${escapeHtml(doc.filename)}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Typ:</span>
            <span class="detail-value">${doc.contentType}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Größe:</span>
            <span class="detail-value">${fileSize}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Pfad:</span>
            <span class="detail-value">${doc.filePath}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Hochgeladen:</span>
            <span class="detail-value">${uploadDate}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Verarbeitet:</span>
            <span class="detail-value">${processedDate}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Status:</span>
            <span class="detail-value">${doc.isProcessed ? 'Verarbeitet' : 'In Bearbeitung'}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">OCR:</span>
            <span class="detail-value">${doc.hasOcr ? 'Verfügbar' : 'Nicht verfügbar'}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Zusammenfassung:</span>
            <span class="detail-value">${doc.hasSummary ? 'Verfügbar' : 'Nicht verfügbar'}</span>
        </div>
        ${doc.tags ? `
        <div class="detail-row">
            <span class="detail-label">Tags:</span>
            <span class="detail-value">${escapeHtml(doc.tags)}</span>
        </div>
        ` : ''}
        ${doc.ocrText ? `
        <div class="detail-row">
            <span class="detail-label">OCR Text:</span>
            <span class="detail-value" style="max-height: 200px; overflow-y: auto; padding: 10px; background: #f5f5f5; border-radius: 5px;">${escapeHtml(doc.ocrText.substring(0, 1000))}${doc.ocrText.length > 1000 ? '...' : ''}</span>
        </div>
        ` : ''}
        ${doc.summary ? `
        <div class="detail-row">
            <span class="detail-label">Zusammenfassung:</span>
            <span class="detail-value" style="padding: 10px; background: #f0f8ff; border-radius: 5px; border-left: 4px solid #667eea;">${escapeHtml(doc.summary)}</span>
        </div>
        ` : ''}
    `;

    documentModal.style.display = 'block';
}

// Delete Document
async function deleteDocument(id) {
    if (!confirm('Möchten Sie dieses Dokument wirklich löschen?')) {
        return;
    }

    try {
        const response = await fetch(`${API_BASE}/document/${id}`, {
            method: 'DELETE'
        });

        if (response.ok) {
            showStatus('Dokument wurde erfolgreich gelöscht', 'success');
            loadAllDocuments(); // Refresh document list
        } else {
            throw new Error('Fehler beim Löschen');
        }
    } catch (error) {
        console.error('Delete error:', error);
        showStatus(`Fehler beim Löschen: ${error.message}`, 'error');
    }
}

// Utility Functions
function showStatus(message, type = 'info') {
    uploadStatus.innerHTML = `<div class="status-message ${type}">${escapeHtml(message)}</div>`;
    setTimeout(() => {
        uploadStatus.innerHTML = '';
    }, 5000);
}

function showLoading(show) {
    loadingSpinner.style.display = show ? 'block' : 'none';
    if (!show) {
        // Clear any existing status messages when loading is done
        setTimeout(() => {
            const existingStatus = uploadStatus.querySelector('.status-message.info');
            if (existingStatus && existingStatus.textContent.includes('Laden')) {
                uploadStatus.innerHTML = '';
            }
        }, 100);
    }
}

function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}