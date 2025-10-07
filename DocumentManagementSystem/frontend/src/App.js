import React, { useEffect, useState } from "react";

function App() {
    const [documents, setDocuments] = useState([]);

    // 👉 States für Text-Dokument
    const [textTitle, setTextTitle] = useState("");
    const [content, setContent] = useState("");

    // 👉 States für Datei-Upload
    const [fileTitle, setFileTitle] = useState("");
    const [file, setFile] = useState(null);

    useEffect(() => {
        fetch("http://localhost:8081/api/document")
            .then((res) => res.json())
            .then((data) => setDocuments(data));
    }, []);

    // Neues Text-Dokument hinzufügen
    const addDocument = async () => {
        await fetch("http://localhost:8081/api/document", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ title: textTitle, content }),
        });
        setTextTitle("");
        setContent("");
        window.location.reload();
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
            window.location.reload();
        };
        reader.readAsDataURL(file);
    };

    return (
        <div style={{ padding: "20px", fontFamily: "Arial" }}>
            <h1>📂 Document Dashboard</h1>

            {/* Text-Dokument */}
            <div style={{ marginBottom: "20px", padding: "10px", border: "1px solid #ccc" }}>
                <h3>📝 Neues Text-Dokument hinzufügen</h3>
                <input
                    placeholder="Titel"
                    value={textTitle}
                    onChange={(e) => setTextTitle(e.target.value)}
                />
                <input
                    placeholder="Content"
                    value={content}
                    onChange={(e) => setContent(e.target.value)}
                />
                <button onClick={addDocument}>➕ Hinzufügen</button>
            </div>

            {/* Datei Upload */}
            <div style={{ marginBottom: "20px", padding: "10px", border: "1px solid #ccc", background: "#eef" }}>
                <h3>📤 Datei hochladen</h3>
                <input
                    placeholder="Titel"
                    value={fileTitle}
                    onChange={(e) => setFileTitle(e.target.value)}
                />
                <input
                    type="file"
                    onChange={(e) => setFile(e.target.files[0])}
                />
                <button onClick={uploadFile}>📎 Upload</button>
            </div>

            {/* Dokumente anzeigen */}
            <h3>📑 Gespeicherte Dokumente</h3>
            <ul>
                {documents.map((doc) => (
                    <li key={doc.id}>
                        <b>{doc.title}</b>: {doc.content}
                    </li>
                ))}
            </ul>
        </div>
    );
}

export default App;
