import React, { useState } from "react";

export default function UploadDocument() {
    const [title, setTitle] = useState("");
    const [file, setFile] = useState(null);
    const [msg, setMsg] = useState("");

    const handleUpload = async (e) => {
        e.preventDefault();                // verhindert Seiten-Reload

        if (!title || !file) {
            setMsg("Bitte Titel und Datei auswählen.");
            return;
        }

        const formData = new FormData();
        formData.append("title", title);   // Namen müssen zum Backend passen
        formData.append("file", file);

        try {
            const res = await fetch("/api/document/upload", {
                method: "POST",
                body: formData,                // KEINE Content-Type Header setzen!
            });

            if (res.ok) {
                setMsg("Upload erfolgreich.");
                setTitle("");
                setFile(null);
            } else {
                const text = await res.text();
                setMsg(`Upload fehlgeschlagen (${res.status}): ${text}`);
            }
        } catch (err) {
            console.error(err);
            setMsg("Server nicht erreichbar.");
        }
    };

    return (
        <div style={{ padding: 16 }}>
            <h3>Datei hochladen</h3>

            {/* WICHTIG: form + onSubmit + type="submit" */}
            <form onSubmit={handleUpload} encType="multipart/form-data">
                <input
                    type="text"
                    name="title"
                    placeholder="Titel"
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    style={{ width: "60%", marginRight: 12 }}
                />
                <input
                    type="file"
                    name="file"
                    onChange={(e) => setFile(e.target.files?.[0] ?? null)}
                    style={{ marginRight: 12 }}
                />
                <button type="submit">Upload</button>
            </form>

            {msg && <p style={{ marginTop: 8 }}>{msg}</p>}
        </div>
    );
}
