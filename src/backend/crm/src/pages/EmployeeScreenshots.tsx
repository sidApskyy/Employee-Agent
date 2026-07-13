import React, { useState } from 'react';
import ScreenshotTimeline from '../components/ScreenshotTimeline';
import ImageViewer from '../components/ImageViewer';
import ScreenshotFilters from '../components/ScreenshotFilters';

interface EmployeeScreenshotsProps {
  employeeId: string;
}

const EmployeeScreenshots: React.FC<EmployeeScreenshotsProps> = ({ employeeId }) => {
  const [selectedScreenshot, setSelectedScreenshot] = useState<any>(null);
  const [filters, setFilters] = useState<any>({
    dateRange: { start: null, end: null },
    monitorId: 'all',
    format: 'all',
    uploadStatus: 'all'
  });

  const handleFilterChange = (newFilters: any) => {
    setFilters(newFilters);
  };

  const handleScreenshotClick = (screenshot: any) => {
    setSelectedScreenshot(screenshot);
  };

  return (
    <div className="employee-screenshots-page">
      <div className="page-header">
        <h2>Employee Screenshots</h2>
        <p>Employee ID: {employeeId}</p>
      </div>

      <ScreenshotFilters onFilterChange={handleFilterChange} />

      <div className="screenshots-content">
        <ScreenshotTimeline
          employeeId={employeeId}
          startDate={filters.dateRange.start}
          endDate={filters.dateRange.end}
          onScreenshotClick={handleScreenshotClick}
        />
      </div>

      {selectedScreenshot && (
        <ImageViewer
          screenshot={selectedScreenshot}
          onClose={() => setSelectedScreenshot(null)}
        />
      )}
    </div>
  );
};

export default EmployeeScreenshots;
