import React, { useEffect, useState } from "react";
import "./App.css";

function App() {
    const [documents, setDocuments] = useState([]);

    const [textTitle, setTextTitle] = useState("");
    const [content, setContent] = useState("");

    const [fileTitle, setFileTitle] = useState("");
    const [file, setFile] = useState(null);

    // Dokumente laden
    const loadDocuments = () => {
        fetch("http://localhost:8081/api/document")
            .then((res) => res.json())
            .then((data) => setDocuments(data))
            .catch((err) => console.error("Fehler beim Laden:", err));
    };

    useEffect(() => {
        loadDocuments();
    }, []);

    // Text-Dokument hinzufügen
    const addDocument = async () => {
        await fetch("http://localhost:8081/api/document", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ title: textTitle, content }),
        });
        setTextTitle("");
        setContent("");
        loadDocuments();
    };

    // Datei hochladen
    const uploadFile = async () => {
        if (!file) return alert("Bitte Datei auswählen!");

        const reader = new FileReader();
        reader.onload = async () => {
            await fetch("http://localhost:8081/api/document", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ title: fileTitle, content: reader.result }),
            });
            setFileTitle("");
            setFile(null);
            loadDocuments();
        };
        reader.readAsDataURL(file);
    };

    // Dokument löschen
    const deleteDocument = async (id) => {
        await fetch(`http://localhost:8081/api/document/${id}`, {
            method: "DELETE",
        });
        loadDocuments();
    };

    return (
        <div className="app-container">
            <h1 className="dashboard-title">Document Dashboard</h1>

            {/* Neues Text-Dokument */}
            <div className="upload-card">
                <h2>Neues Text-Dokument</h2>
                <div className="form-group">
                    <input
                        className="input-field"
                        placeholder="Titel"
                        value={textTitle}
                        onChange={(e) => setTextTitle(e.target.value)}
                    />
                    <input
                        className="input-field"
                        placeholder="Inhalt"
                        value={content}
                        onChange={(e) => setContent(e.target.value)}
                    />
                    <button className="btn btn-primary" onClick={addDocument}>
                        Hinzufügen
                    </button>
                </div>
            </div>

            {/* Datei Upload */}
            <div className="upload-card">
                <h2>Datei hochladen</h2>
                <div className="form-group">
                    <input
                        className="input-field"
                        placeholder="Titel"
                        value={fileTitle}
                        onChange={(e) => setFileTitle(e.target.value)}
                    />
                    <input
                        className="file-input"
                        type="file"
                        onChange={(e) => setFile(e.target.files[0])}
                    />
                    <button className="btn btn-primary" onClick={uploadFile}>
                        Upload
                    </button>
                </div>
            </div>

            {/* Gespeicherte Dokumente */}
            <h2 className="docs-title">Gespeicherte Dokumente</h2>
            <div className="docs-grid">
                {documents.map((doc) => (
                    <div className="doc-card" key={doc.id}>
                        <h3>{doc.title}</h3>
                        <p className="doc-id">ID: {doc.id}</p>
                        <p className="doc-content">
                            {doc.content?.startsWith("data:application/pdf")
                                ? "PDF-Datei gespeichert"
                                : doc.content?.substring(0, 100) + "..."}
                        </p>
                        <button className="btn btn-danger" onClick={() => deleteDocument(doc.id)}>
                            Löschen
                        </button>
                    </div>
                ))}
            </div>
        </div>
    );
}

export default App;
