export interface NavigationLink {
  label: string;
  route: string;
}

export interface NavigationItem {
  label: string;
  icon: string;
  /** Set for a single top-level destination. */
  route?: string;
  /** Set for a collapsible group of destinations. */
  links?: NavigationLink[];
}
