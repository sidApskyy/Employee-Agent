import React, { useState, useEffect } from 'react';

interface ImageViewerProps {
  screenshot: {
    id: string;
    storagePath: string;
    width: number;
    height: number;
    format: string;
  } | null;
  onClose: () => void;
}

const ImageViewer: React.FC<ImageViewerProps> = ({ screenshot, onClose }) => {
  const [imageUrl, setImageUrl] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (screenshot) {
      loadImage();
    }
  }, [screenshot]);

  const loadImage = async () => {
    if (!screenshot) return;

    try {
      setLoading(true);
      setError(null);
      const response = await fetch(`/api/screenshots/image/${screenshot.id}`);
      if (!response.ok) throw new Error('Failed to fetch image URL');
      const data = await response.json();
      setImageUrl(data.url);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load image');
    } finally {
      setLoading(false);
    }
  };

  if (!screenshot) return null;

  return (
    <div className="image-viewer-overlay" onClick={onClose}>
      <div className="image-viewer-content" onClick={(e) => e.stopPropagation()}>
        <div className="image-viewer-header">
          <h3>Screenshot Viewer</h3>
          <button className="close-button" onClick={onClose}>×</button>
        </div>
        <div className="image-viewer-body">
          {loading && <div className="loading">Loading image...</div>}
          {error && <div className="error">Error: {error}</div>}
          {imageUrl && (
            <img
              src={imageUrl}
              alt={`Screenshot ${screenshot.id}`}
              className="screenshot-image"
            />
          )}
        </div>
        <div className="image-viewer-footer">
          <div className="image-info">
            <span>Resolution: {screenshot.width}x{screenshot.height}</span>
            <span>Format: {screenshot.format.toUpperCase()}</span>
            <span>Path: {screenshot.storagePath}</span>
          </div>
          <div className="image-actions">
            <button onClick={() => window.open(imageUrl, '_blank')}>Open in New Tab</button>
            <button onClick={() => {/* Download functionality */}}>Download</button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ImageViewer;
