import { getStatusDisplay } from './status-display';

describe('getStatusDisplay', () => {
  it('maps customer statuses to business tones', () => {
    expect(getStatusDisplay('Normal')).toEqual({ label: 'Normal', tone: 'success' });
    expect(getStatusDisplay('Suspended')).toEqual({ label: 'Suspended', tone: 'warning' });
    expect(getStatusDisplay('Blocked')).toEqual({ label: 'Blocked', tone: 'danger' });
  });

  it('maps product and category lifecycle statuses consistently', () => {
    expect(getStatusDisplay('Draft')).toEqual({ label: 'Draft', tone: 'neutral' });
    expect(getStatusDisplay('Published')).toEqual({ label: 'Published', tone: 'success' });
    expect(getStatusDisplay('Discontinued')).toEqual({ label: 'Discontinued', tone: 'warning' });
    expect(getStatusDisplay('Archived')).toEqual({ label: 'Archived', tone: 'neutral' });
  });
});
