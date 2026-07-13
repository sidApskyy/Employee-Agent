import React, { useState, useEffect } from 'react';
import { format } from 'date-fns';

interface Screenshot {
  id: string;
  captureTimeUtc: string;
  monitorId: string;
  width: number;
  height: number;
  format: string;
  storagePath: string;
  uploadStatus: string;
}

interface ScreenshotTimelineProps {
  employeeId: string;
  startDate?: Date;
  endDate?: Date;
  onScreenshotClick?: (screenshot: Screenshot) => void;
}

const ScreenshotTimeline: React.FC<ScreenshotTimelineProps> = ({
  employeeId,
  startDate,
  endDate,
  onScreenshotClick
}) => {
  const [screenshots, setScreenshots] = useState<Screenshot[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchScreenshots();
  }, [employeeId, startDate, endDate]);

  const fetchScreenshots = async () => {
    try {
      setLoading(true);
      const params = new URLSearchParams();
      if (startDate) params.append('startDate', startDate.toISOString());
      if (endDate) params.append('endDate', endDate.toISOString());

      const response = await fetch(`/api/screenshots/employee/${employeeId}/timeline?${params}`);
      if (!response.ok) throw new Error('Failed to fetch screenshots');

      const data = await response.json();
      setScreenshots(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div>Loading screenshots...</div>;
  if (error) return <div>Error: {error}</div>;

  return (
    <div className="screenshot-timeline">
      <h3>Screenshot Timeline</h3>
      <div className="timeline-container">
        {screenshots.map((screenshot) => (
          <div
            key={screenshot.id}
            className="timeline-item"
            onClick={() => onScreenshotClick?.(screenshot)}
          >
            <div className="timestamp">
              {format(new Date(screenshot.captureTimeUtc), 'MMM dd, HH:mm')}
            </div>
            <div className="screenshot-info">
              <span className="monitor">Monitor {screenshot.monitorId}</span>
              <span className="resolution">{screenshot.width}x{screenshot.height}</span>
              <span className="format">{screenshot.format.toUpperCase()}</span>
              <span className={`status ${screenshot.uploadStatus.toLowerCase()}`}>
                {screenshot.uploadStatus}
              </span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

export default ScreenshotTimeline;
