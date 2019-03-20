<%@ Page Language="C#" MasterPageFile="Site.Master" AutoEventWireup="true" Inherits="Rock.Web.UI.RockPage" %>

<asp:Content ID="ctMain" ContentPlaceHolderID="main" runat="server">

    <Rock:Lava ID="PageColor" runat="server">
            {% assign pageColor = CurrentPage | Attribute:'PageColor' %}
            <div class="position-fixed top-zero right-zero bottom-zero left-zero" style="background-color: {{ pageColor }}; z-index: -1;"></div>
    </Rock:Lava>

    <div class="soft xs-soft-half hard-bottom xs-hard-bottom clearfix">

        <Rock:Lava ID="PageTitle" runat="server">
        {% if CurrentPage.PageDisplayTitle == true %}
            {[pageHeader]}
        {% endif %}
        </Rock:Lava>

        <!-- Breadcrumbs -->
        <Rock:PageBreadCrumbs ID="PageBreadCrumbs" runat="server" />

        <!-- Ajax Error -->
        <div class="alert alert-danger ajax-error no-index" style="display:none">
            <p><strong>Error</strong></p>
            <span class="ajax-error-message"></span>
        </div>

        <Rock:Zone Name="Feature" runat="server" />

        <div class="row">
            <div class="col-lg-9 col-md-8 col-sm-7 col-xs-12">
                <Rock:Zone Name="Main" runat="server" />
            </div><div class="col-lg-3 col-md-4 col-sm-5 col-xs-12">
                <Rock:Zone Name="Section A" runat="server" />
            </div>
        </div>
        
        <Rock:Zone Name="Section B" runat="server" />
        <Rock:Zone Name="Section C" runat="server" />
        <Rock:Zone Name="Section D" runat="server" />

    </div>

</asp:Content>