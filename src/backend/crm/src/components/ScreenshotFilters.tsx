import React from 'react';

interface ScreenshotFiltersProps {
  onFilterChange: (filters: FilterState) => void;
}

interface FilterState {
  dateRange: { start: Date | null; end: Date | null };
  monitorId: string;
  format: string;
  uploadStatus: string;
}

const ScreenshotFilters: React.FC<ScreenshotFiltersProps> = ({ onFilterChange }) => {
  const [filters, setFilters] = React.useState<FilterState>({
    dateRange: { start: null, end: null },
    monitorId: 'all',
    format: 'all',
    uploadStatus: 'all'
  });

  const handleFilterChange = (key: keyof FilterState, value: any) => {
    const newFilters = { ...filters, [key]: value };
    setFilters(newFilters);
    onFilterChange(newFilters);
  };

  return (
    <div className="screenshot-filters">
      <div className="filter-group">
        <label>Date Range:</label>
        <input
          type="date"
          value={filters.dateRange.start ? filters.dateRange.start.toISOString().split('T')[0] : ''}
          onChange={(e) => handleFilterChange('dateRange', { ...filters.dateRange, start: e.target.value ? new Date(e.target.value) : null })}
        />
        <span>to</span>
        <input
          type="date"
          value={filters.dateRange.end ? filters.dateRange.end.toISOString().split('T')[0] : ''}
          onChange={(e) => handleFilterChange('dateRange', { ...filters.dateRange, end: e.target.value ? new Date(e.target.value) : null })}
        />
      </div>

      <div className="filter-group">
        <label>Monitor:</label>
        <select
          value={filters.monitorId}
          onChange={(e) => handleFilterChange('monitorId', e.target.value)}
        >
          <option value="all">All Monitors</option>
          <option value="0">Primary</option>
          <option value="1">Secondary</option>
        </select>
      </div>

      <div className="filter-group">
        <label>Format:</label>
        <select
          value={filters.format}
          onChange={(e) => handleFilterChange('format', e.target.value)}
        >
          <option value="all">All Formats</option>
          <option value="jpg">JPEG</option>
          <option value="png">PNG</option>
          <option value="webp">WEBP</option>
        </select>
      </div>

      <div className="filter-group">
        <label>Status:</label>
        <select
          value={filters.uploadStatus}
          onChange={(e) => handleFilterChange('uploadStatus', e.target.value)}
        >
          <option value="all">All Status</option>
          <option value="Pending">Pending</option>
          <option value="Completed">Completed</option>
          <option value="Failed">Failed</option>
        </select>
      </div>

      <button
        className="reset-button"
        onClick={() => {
          const resetFilters: FilterState = {
            dateRange: { start: null, end: null },
            monitorId: 'all',
            format: 'all',
            uploadStatus: 'all'
          };
          setFilters(resetFilters);
          onFilterChange(resetFilters);
        }}
      >
        Reset Filters
      </button>
    </div>
  );
};

export default ScreenshotFilters;
